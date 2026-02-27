from flask import Flask, request, jsonify
from flask_cors import CORS
from flask_sqlalchemy import SQLAlchemy
from sqlalchemy.sql import func
import os

app = Flask(__name__)
CORS(app)

# Default to SQLite for local development.
# Can be overridden with DATABASE_URL for PostgreSQL/MySQL.
app.config["SQLALCHEMY_DATABASE_URI"] = os.getenv(
    "DATABASE_URL", "sqlite:///unemployed_simulator.db"
)
app.config["SQLALCHEMY_TRACK_MODIFICATIONS"] = False

db = SQLAlchemy(app)


class Player(db.Model):
    __tablename__ = "players"

    id = db.Column(db.Integer, primary_key=True)
    username = db.Column(db.String(32), unique=True, nullable=False, index=True)
    score = db.Column(db.Integer, nullable=False, default=0)
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
    company_id = db.Column(db.Integer, nullable=True, index=True)
    message = db.Column(db.Text, nullable=False)
    status = db.Column(db.String(20), nullable=False, default="submitted")
    created_at = db.Column(db.DateTime(timezone=True), nullable=False, server_default=func.now())
    updated_at = db.Column(
        db.DateTime(timezone=True), nullable=False, server_default=func.now(), onupdate=func.now()
    )


@app.route("/player/<int:id>", methods=["GET"])
def get_player(id):
    player = Player.query.get(id)
    if not player:
        return jsonify({"error": "Player not found"}), 404

    return jsonify({
        "id": player.id,
    })


@app.route("/player/update-score", methods=["POST"])
def update_score():
    data = request.json
    player_id = data.get("player_id")
    new_score = data.get("score")

    player = Player.query.get(player_id)
    if not player:
        return jsonify({"error": "Player not found"}), 404

    player.score = new_score
    db.session.commit()
    return jsonify({"updated": True, "new_score": player.score})


@app.route("/apply", methods=["POST"])
def apply():
    data = request.json
    player_id = data.get("player_id")
    message = data.get("message")

    player = Player.query.get(player_id)
    if not player:
        return jsonify({"error": "Player not found"}), 404

    application = Application(
        player_id=player_id,
        message=message
    )

    db.session.add(application)
    db.session.commit()

    return jsonify({"application_submitted": True})


@app.route("/applications", methods=["GET"])
def get_applications():
    applications = Application.query.all()

    results = []
    for app_row in applications:
        results.append({
            "id": app_row.id,
            "player_id": app_row.player_id,
            "message": app_row.message,
            "created_at": app_row.created_at.isoformat()
        })

    return jsonify(results)


if __name__ == "__main__":
    with app.app_context():
        db.create_all()
    app.run(debug=True, port=8000)
