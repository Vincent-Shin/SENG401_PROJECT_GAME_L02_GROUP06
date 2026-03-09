import os
import random
import json

from flask import Flask, jsonify, request
from flask_cors import CORS
from flask_sqlalchemy import SQLAlchemy
from sqlalchemy.sql import func

app = Flask(__name__)
CORS(app)

# Default to SQLite for local development.
# Can be overridden with DATABASE_URL for PostgreSQL/MySQL.
app.config["SQLALCHEMY_DATABASE_URI"] = os.getenv(
    "DATABASE_URL", "sqlite:///unemployed_simulator.db"
)
app.config["SQLALCHEMY_TRACK_MODIFICATIONS"] = False

db = SQLAlchemy(app)

MAX_FAILED_APPLICATIONS = 3
MAX_RESUME_SCORE = 100

COMPANY_RULES = {
    "startup": {
        "minimum_score": 50,
        "experience_gain": 3,
        "apply_multiplier": 0.45,
        "required_activities": [],
        "required_successful_company_tiers": [],
    },
    "mid_tier": {
        "minimum_score": 65,
        "experience_gain": 6,
        "apply_multiplier": 0.35,
        "required_activities": ["project", "certificate"],
        "required_successful_company_tiers": ["startup"],
    },
    "big_tech": {
        "minimum_score": 85,
        "experience_gain": 10,
        "apply_multiplier": 0.25,
        "required_activities": ["project", "networking", "work_experience", "certificate"],
        "required_successful_company_tiers": ["startup", "mid_tier"],
    },
}


class Player(db.Model):
    __tablename__ = "players"

    id = db.Column(db.Integer, primary_key=True)
    username = db.Column(db.String(32), unique=True, nullable=False, index=True)
    score = db.Column(db.Integer, nullable=False, default=50)
    current_stage = db.Column(db.String(50), nullable=False, default="intro")
    failed_applications = db.Column(db.Integer, nullable=False, default=0)
    mentor_count = db.Column(db.Integer, nullable=False, default=0)
    networking_count = db.Column(db.Integer, nullable=False, default=0)
    completed_activity_ids = db.Column(db.Text, nullable=False, default="[]")
    completed_project = db.Column(db.Boolean, nullable=False, default=False)
    completed_certificate = db.Column(db.Boolean, nullable=False, default=False)
    completed_resume_tailored = db.Column(db.Boolean, nullable=False, default=False)
    completed_networking = db.Column(db.Boolean, nullable=False, default=False)
    completed_work_experience = db.Column(db.Boolean, nullable=False, default=False)
    is_game_over = db.Column(db.Boolean, nullable=False, default=False)
    is_employed = db.Column(db.Boolean, nullable=False, default=False)
    employed_company_tier = db.Column(db.String(20), nullable=True)
    created_at = db.Column(db.DateTime(timezone=True), nullable=False, server_default=func.now())
    updated_at = db.Column(
        db.DateTime(timezone=True), nullable=False, server_default=func.now(), onupdate=func.now()
    )


class Application(db.Model):
    __tablename__ = "applications"

    id = db.Column(db.Integer, primary_key=True)
    player_id = db.Column(
        db.Integer, db.ForeignKey("players.id", ondelete="CASCADE"), nullable=False, index=True
    )
    company_tier = db.Column(db.String(20), nullable=False, index=True)
    company_id = db.Column(db.Integer, nullable=True, index=True)
    market_percent = db.Column(db.Float, nullable=False, default=0)
    market_multiplier = db.Column(db.Float, nullable=False, default=1)
    score_snapshot = db.Column(db.Integer, nullable=False, default=0)
    resume_multiplier = db.Column(db.Float, nullable=False, default=1)
    interview_probability = db.Column(db.Float, nullable=False, default=0)
    score_delta = db.Column(db.Integer, nullable=False, default=0)
    failure_count_after = db.Column(db.Integer, nullable=False, default=0)
    message = db.Column(db.Text, nullable=False, default="")
    status = db.Column(db.String(32), nullable=False, default="submitted")
    created_at = db.Column(db.DateTime(timezone=True), nullable=False, server_default=func.now())
    updated_at = db.Column(
        db.DateTime(timezone=True), nullable=False, server_default=func.now(), onupdate=func.now()
    )


def clamp(value, minimum, maximum):
    return max(minimum, min(value, maximum))


def get_company_rule(company_tier):
    return COMPANY_RULES.get((company_tier or "").lower())


def clamp_resume_score(score):
    return clamp(int(score), 0, MAX_RESUME_SCORE)


def calculate_base_rate(score):
    if score < 50:
        return 0.0
    if score < 65:
        return 0.4
    if score < 85:
        return 0.6
    return 1.0


def calculate_resume_multiplier(player):
    bonus = (player.mentor_count * 0.10) + (player.networking_count * 0.05)
    return 1.0 + min(bonus, 0.30)


def get_completed_activity_ids(player):
    try:
        data = json.loads(player.completed_activity_ids or "[]")
        return data if isinstance(data, list) else []
    except (TypeError, ValueError, json.JSONDecodeError):
        return []


def set_completed_activity_ids(player, activity_ids):
    player.completed_activity_ids = json.dumps(sorted(set(activity_ids)))


def get_successful_company_tiers(player_id):
    successful_statuses = ["interview_scheduled", "offer_made", "offer_accepted"]
    rows = (
        db.session.query(Application.company_tier)
        .filter_by(player_id=player_id)
        .filter(Application.status.in_(successful_statuses))
        .distinct()
        .all()
    )
    return [row[0] for row in rows]


def serialize_player(player):
    attempts_left = max(0, MAX_FAILED_APPLICATIONS - player.failed_applications)
    return {
        "id": player.id,
        "username": player.username,
        "score": player.score,
        "current_stage": player.current_stage,
        "failed_applications": player.failed_applications,
        "apply_attempts_left": attempts_left,
        "mentor_count": player.mentor_count,
        "networking_count": player.networking_count,
        "completed_activity_ids": get_completed_activity_ids(player),
        "completed_project": player.completed_project,
        "completed_certificate": player.completed_certificate,
        "completed_resume_tailored": player.completed_resume_tailored,
        "completed_networking": player.completed_networking,
        "completed_work_experience": player.completed_work_experience,
        "successful_company_tiers": get_successful_company_tiers(player.id),
        "resume_multiplier": round(calculate_resume_multiplier(player), 3),
        "is_game_over": player.is_game_over,
        "is_employed": player.is_employed,
        "employed_company_tier": player.employed_company_tier,
        "created_at": player.created_at.isoformat() if player.created_at else None,
        "updated_at": player.updated_at.isoformat() if player.updated_at else None,
    }


def serialize_application(application):
    return {
        "id": application.id,
        "player_id": application.player_id,
        "company_tier": application.company_tier,
        "company_id": application.company_id,
        "market_percent": application.market_percent,
        "market_multiplier": application.market_multiplier,
        "score_snapshot": application.score_snapshot,
        "resume_multiplier": application.resume_multiplier,
        "interview_probability": application.interview_probability,
        "score_delta": application.score_delta,
        "failure_count_after": application.failure_count_after,
        "message": application.message,
        "status": application.status,
        "created_at": application.created_at.isoformat() if application.created_at else None,
        "updated_at": application.updated_at.isoformat() if application.updated_at else None,
    }


def get_player_activity_completion(player, activity_type):
    activity_type = (activity_type or "").strip().lower()
    if activity_type == "project":
        return player.completed_project
    if activity_type == "certificate":
        return player.completed_certificate
    if activity_type in ("resume_tailored", "resume"):
        return player.completed_resume_tailored
    if activity_type == "networking":
        return player.completed_networking
    if activity_type == "work_experience":
        return player.completed_work_experience
    return None


def set_player_activity_completion(player, activity_type, completed=True):
    activity_type = (activity_type or "").strip().lower()
    if activity_type == "project":
        player.completed_project = completed
        return True
    if activity_type == "certificate":
        player.completed_certificate = completed
        return True
    if activity_type in ("resume_tailored", "resume"):
        player.completed_resume_tailored = completed
        return True
    if activity_type == "networking":
        player.completed_networking = completed
        return True
    if activity_type == "work_experience":
        player.completed_work_experience = completed
        return True
    return False


def get_missing_activities(player, company_rule):
    required = company_rule.get("required_activities", [])
    return [
        activity
        for activity in required
        if not get_player_activity_completion(player, activity)
    ]


def has_successful_application(player_id, company_tier):
    successful_statuses = ["interview_scheduled", "offer_made", "offer_accepted"]
    return (
        Application.query.filter_by(player_id=player_id, company_tier=company_tier)
        .filter(Application.status.in_(successful_statuses))
        .first()
        is not None
    )


def format_company_tier_requirement(company_tier):
    labels = {
        "startup": "Startup company experience",
        "mid_tier": "Mid-tier company experience",
        "big_tech": "Big Tech company experience",
    }
    return labels.get(company_tier, company_tier)


def get_missing_company_tier_requirements(player, company_rule):
    required = company_rule.get("required_successful_company_tiers", [])
    return [
        format_company_tier_requirement(company_tier)
        for company_tier in required
        if not has_successful_application(player.id, company_tier)
    ]


@app.route("/health", methods=["GET"])
def health():
    return jsonify({"ok": True})


@app.route("/player/<int:id>", methods=["GET"])
def get_player(id):
    player = db.session.get(Player, id)
    if not player:
        return jsonify({"error": "Player not found"}), 404

    return jsonify(serialize_player(player))


@app.route("/player/load-or-create", methods=["POST"])
def load_or_create_player():
    data = request.get_json(silent=True) or {}
    username = (data.get("username") or "").strip()

    if not username:
        return jsonify({"error": "Username is required"}), 400

    player = Player.query.filter_by(username=username).first()
    created = False

    if player is None:
        player = Player(username=username, score=50)
        db.session.add(player)
        db.session.commit()
        created = True

    return jsonify({
        "created": created,
        "player": serialize_player(player),
    })


@app.route("/player/update-score", methods=["POST"])
def update_score():
    data = request.get_json(silent=True) or {}
    player_id = data.get("player_id")
    new_score = data.get("score")

    if player_id is None or new_score is None:
        return jsonify({"error": "player_id and score are required"}), 400

    player = db.session.get(Player, player_id)
    if not player:
        return jsonify({"error": "Player not found"}), 404

    player.score = clamp_resume_score(new_score)
    db.session.commit()
    return jsonify({"updated": True, "player": serialize_player(player)})


@app.route("/player/update-stage", methods=["POST"])
def update_stage():
    data = request.get_json(silent=True) or {}
    player_id = data.get("player_id")
    stage = (data.get("stage") or "").strip()

    if player_id is None or not stage:
        return jsonify({"error": "player_id and stage are required"}), 400

    player = db.session.get(Player, player_id)
    if not player:
        return jsonify({"error": "Player not found"}), 404

    player.current_stage = stage
    db.session.commit()
    return jsonify({"updated": True, "player": serialize_player(player)})


@app.route("/player/add-bonus", methods=["POST"])
def add_bonus():
    data = request.get_json(silent=True) or {}
    player_id = data.get("player_id")
    mentor_delta = int(data.get("mentor_delta", 0))
    networking_delta = int(data.get("networking_delta", 0))

    if player_id is None:
        return jsonify({"error": "player_id is required"}), 400

    player = db.session.get(Player, player_id)
    if not player:
        return jsonify({"error": "Player not found"}), 404

    player.mentor_count = max(0, player.mentor_count + mentor_delta)
    player.networking_count = max(0, player.networking_count + networking_delta)
    db.session.commit()

    return jsonify({"updated": True, "player": serialize_player(player)})


@app.route("/player/complete-activity", methods=["POST"])
def complete_activity():
    data = request.get_json(silent=True) or {}
    player_id = data.get("player_id")
    activity_id = (data.get("activity_id") or "").strip().lower()
    activity_type = (data.get("activity_type") or "").strip().lower()
    score_delta = int(data.get("score_delta", 0))
    one_time_only = bool(data.get("one_time_only", True))

    if player_id is None or not activity_id or not activity_type:
        return jsonify({"error": "player_id, activity_id and activity_type are required"}), 400

    player = db.session.get(Player, player_id)
    if not player:
        return jsonify({"error": "Player not found"}), 404

    if player.is_game_over:
        return jsonify({"error": "Player is already game over", "player": serialize_player(player)}), 400

    activity_completion_flag = get_player_activity_completion(player, activity_type)
    if activity_completion_flag is None:
        return jsonify({"error": "Invalid activity_type"}), 400

    completed_activity_ids = get_completed_activity_ids(player)
    already_completed = activity_id in completed_activity_ids

    if one_time_only and already_completed:
        return jsonify({
            "updated": False,
            "already_completed": True,
            "player": serialize_player(player),
        })

    player.score = clamp_resume_score(player.score + max(0, score_delta))
    set_player_activity_completion(player, activity_type, True)
    completed_activity_ids.append(activity_id)
    set_completed_activity_ids(player, completed_activity_ids)

    if activity_type == "networking":
        networking_completed = [
            completed_id for completed_id in get_completed_activity_ids(player)
            if completed_id.startswith("networking")
        ]
        player.networking_count = max(player.networking_count, len(networking_completed))

    db.session.commit()

    return jsonify({
        "updated": True,
        "already_completed": False,
        "player": serialize_player(player),
    })


@app.route("/player/reset-run", methods=["POST"])
def reset_run():
    data = request.get_json(silent=True) or {}
    player_id = data.get("player_id")

    if player_id is None:
        return jsonify({"error": "player_id is required"}), 400

    player = db.session.get(Player, player_id)
    if not player:
        return jsonify({"error": "Player not found"}), 404

    player.score = clamp_resume_score(50)
    player.current_stage = "intro"
    player.failed_applications = 0
    player.mentor_count = 0
    player.networking_count = 0
    player.completed_activity_ids = "[]"
    player.completed_project = False
    player.completed_certificate = False
    player.completed_resume_tailored = False
    player.completed_networking = False
    player.completed_work_experience = False
    player.is_game_over = False
    player.is_employed = False
    player.employed_company_tier = None
    Application.query.filter_by(player_id=player.id).delete()
    db.session.commit()

    return jsonify({"reset": True, "player": serialize_player(player)})


@app.route("/player/delete", methods=["POST"])
def delete_player():
    data = request.get_json(silent=True) or {}
    player_id = data.get("player_id")

    if player_id is None:
        return jsonify({"error": "player_id is required"}), 400

    player = db.session.get(Player, player_id)
    if not player:
        return jsonify({"error": "Player not found"}), 404

    username = player.username
    db.session.delete(player)
    db.session.commit()

    return jsonify({"deleted": True, "username": username})


@app.route("/apply", methods=["POST"])
def apply():
    data = request.get_json(silent=True) or {}
    player_id = data.get("player_id")
    company_tier = (data.get("company_tier") or "").strip().lower()
    message = data.get("message", "")
    market_percent = float(data.get("market_percent", 0))
    market_multiplier = float(data.get("market_multiplier", 1))

    if player_id is None or not company_tier:
        return jsonify({"error": "player_id and company_tier are required"}), 400

    player = db.session.get(Player, player_id)
    if not player:
        return jsonify({"error": "Player not found"}), 404

    if player.is_game_over:
        return jsonify({"error": "Player is already game over", "player": serialize_player(player)}), 400

    if player.is_employed:
        return jsonify({"error": "Player already accepted a job", "player": serialize_player(player)}), 400

    company_rule = get_company_rule(company_tier)
    if company_rule is None:
        return jsonify({"error": "Invalid company_tier"}), 400

    missing_activities = get_missing_activities(player, company_rule)
    missing_company_tiers = get_missing_company_tier_requirements(player, company_rule)
    missing_requirements = missing_activities + missing_company_tiers
    if missing_requirements:
        return jsonify({
            "error": "Missing required activities",
            "reason": "missing_required_activities",
            "missing_activities": missing_requirements,
            "player": serialize_player(player),
        }), 400

    if has_successful_application(player.id, company_tier):
        return jsonify({
            "error": "You already completed this company tier",
            "reason": "already_applied_to_tier",
            "player": serialize_player(player),
        }), 400

    base_rate = calculate_base_rate(player.score)
    resume_multiplier = calculate_resume_multiplier(player)
    score_snapshot = player.score
    company_multiplier = float(company_rule.get("apply_multiplier", 1.0))
    probability = clamp(base_rate * market_multiplier * resume_multiplier * company_multiplier, 0.0, 1.0)

    application = Application(
        player_id=player.id,
        company_tier=company_tier,
        market_percent=market_percent,
        market_multiplier=market_multiplier,
        score_snapshot=score_snapshot,
        resume_multiplier=resume_multiplier,
        interview_probability=round(probability * 100, 2),
        message=message,
    )

    if player.score < company_rule["minimum_score"]:
        application.status = "blocked_min_score"
        application.failure_count_after = player.failed_applications
        db.session.add(application)
        db.session.commit()

        return jsonify({
            "application_submitted": True,
            "result": "rejected",
            "reason": "resume_below_requirement",
            "minimum_score": company_rule["minimum_score"],
            "player": serialize_player(player),
            "application": serialize_application(application),
        })

    success = random.random() < probability

    if success:
        score_delta = company_rule["experience_gain"]
        player.score = clamp_resume_score(player.score + score_delta)
        player.current_stage = f"{company_tier}_applied_successfully"
        player.is_employed = company_tier == "big_tech"
        if player.is_employed:
            player.employed_company_tier = company_tier
        application.status = "offer_made" if player.is_employed else "interview_scheduled"
        application.score_delta = score_delta
        application.failure_count_after = player.failed_applications
        result = "success"
    else:
        player.failed_applications += 1
        player.is_game_over = player.failed_applications >= MAX_FAILED_APPLICATIONS
        application.status = "rejected"
        application.failure_count_after = player.failed_applications
        result = "failure"

    db.session.add(application)
    db.session.commit()

    return jsonify({
        "application_submitted": True,
        "result": result,
        "player": serialize_player(player),
        "application": serialize_application(application),
    })


@app.route("/applications", methods=["GET"])
def get_applications():
    player_id = request.args.get("player_id", type=int)
    query = Application.query.order_by(Application.created_at.desc())
    if player_id is not None:
        query = query.filter_by(player_id=player_id)

    return jsonify([serialize_application(application) for application in query.all()])


if __name__ == "__main__":
    with app.app_context():
        db.create_all()
    app.run(debug=True, port=8000)
