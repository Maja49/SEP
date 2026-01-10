from flask_sqlalchemy import SQLAlchemy
from datetime import datetime

db = SQLAlchemy()

class User(db.Model):
    __tablename__ = "users"

    id = db.Column(db.Integer, primary_key=True)
    username = db.Column(db.String(50), unique=True, nullable=False, index=True)
    password_hash = db.Column(db.String(255), nullable=False)
    created_at = db.Column(db.DateTime, default=datetime.utcnow, nullable=False)
    is_admin = db.Column(db.Boolean, default=False, nullable=False)


class Package(db.Model):
    __tablename__ = "packages"

    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(120), nullable=False)
    price = db.Column(db.Integer, nullable=False)  # u najmanjim jedinicama npr. RSD
    currency = db.Column(db.String(3), nullable=False, default="RSD")
    is_active = db.Column(db.Boolean, default=True, nullable=False)

class Order(db.Model):
    __tablename__ = "orders"

    id = db.Column(db.Integer, primary_key=True)
    user_id = db.Column(db.Integer, db.ForeignKey("users.id"), nullable=False)
    package_id = db.Column(db.Integer, db.ForeignKey("packages.id"), nullable=False)

    status = db.Column(db.String(20), nullable=False, default="CREATED")  # CREATED, SUCCESS, FAILED
    payment_method = db.Column(db.String(20), nullable=True)  # CARD, QR
    created_at = db.Column(db.DateTime, default=datetime.utcnow, nullable=False)

    user = db.relationship("User")
    package = db.relationship("Package")
