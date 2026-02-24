from flask import Flask, request, jsonify
from flask_cors import CORS
import datetime

app = Flask(__name__)
CORS(app)

# DATABASE IS NOT IMPLEMENTED YET


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
    app.run(debug=True, port=8000)