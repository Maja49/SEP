from flask import Flask, redirect, url_for, render_template, request, session
from werkzeug.security import generate_password_hash, check_password_hash
import requests
from datetime import datetime, timezone

from models import db, User, Package, Order

app = Flask(__name__)
app.secret_key = "dev-secret-change-this"
PSP_BASE_URL = "https://localhost:7098"  


# MySQL connection string: mysql+pymysql://USER:PASS@HOST:PORT/DB_NAME
app.config["SQLALCHEMY_DATABASE_URI"] = "mysql+pymysql://root:2310@localhost:3306/sep_webshop"
app.config["SQLALCHEMY_TRACK_MODIFICATIONS"] = False

db.init_app(app)

def require_login():
    return session.get("user_id") is not None

@app.route("/")
def home():
    if require_login():
        return redirect(url_for("packages"))
    return redirect(url_for("login"))

@app.route("/register", methods=["GET", "POST"])
def register():
    if request.method == "POST":
        username = request.form.get("username", "").strip()
        password = request.form.get("password", "").strip()

        if not username or not password:
            return render_template("register.html", error="Popuni sva polja.")

        if len(username) < 3:
            return render_template("register.html", error="Username mora imati bar 3 karaktera.")
        if len(password) < 6:
            return render_template("register.html", error="Lozinka mora imati bar 6 karaktera.")

        existing = User.query.filter_by(username=username).first()
        if existing:
            return render_template("register.html", error="Korisničko ime je zauzeto.")

        u = User(username=username, password_hash=generate_password_hash(password))
        db.session.add(u)
        db.session.commit()

        # auto-login posle registracije
        session["user_id"] = u.id
        session["username"] = u.username
        session["is_admin"] = bool(u.is_admin)

        return redirect(url_for("packages"))

    return render_template("register.html")

@app.route("/login", methods=["GET", "POST"])
def login():
    if request.method == "POST":
        username = request.form.get("username", "").strip()
        password = request.form.get("password", "").strip()

        if not username or not password:
            return render_template("login.html", error="Unesi username i lozinku.")

        u = User.query.filter_by(username=username).first()
        if not u:
            return render_template("login.html", error="Pogrešan username ili lozinka.")
        if not check_password_hash(u.password_hash, password):
            return render_template("login.html", error="Pogrešan username ili lozinka.")

        session["user_id"] = u.id
        session["username"] = u.username
        session["is_admin"] = bool(u.is_admin)
        return redirect(url_for("packages"))

    return render_template("login.html")

@app.route("/logout")
def logout():
    session.pop("user_id", None)
    session.pop("username", None)
    session.pop("is_admin", None)

    return redirect(url_for("login"))

@app.route("/packages")
def packages():
    if not require_login():
        return redirect(url_for("login"))

    packages = Package.query.filter_by(is_active=True).all()
    return render_template("packages.html", user=session.get("username"), packages=packages)

@app.route("/history")
def history():
    if not require_login():
        return redirect(url_for("login"))

    orders = (
        Order.query
        .filter_by(user_id=session["user_id"])
        .order_by(Order.created_at.desc())
        .all()
    )
    return render_template("history.html", user=session.get("username"), orders=orders)

@app.route("/admin")
def admin_panel():
    if not require_login():
        return redirect(url_for("login"))
    if not require_admin():
        return "Forbidden", 403
    return "Admin panel (placeholder)", 200

@app.route("/buy/<int:package_id>")
def buy_package(package_id: int):
    if not require_login():
        return redirect(url_for("login"))

    p = Package.query.get_or_404(package_id)

    # 1) napravi order u bazi
    order = Order(
        user_id=session["user_id"],
        package_id=p.id,
        status="CREATED",
        payment_method=None
    )
    db.session.add(order)
    db.session.commit()

    # 2) pozovi PSP init (Tabela 1)
    payload = {
        "merchantId": "webshop123",
        "merchantPassword": "secret",
        "amount": p.price,
        "currency": p.currency,
        "merchantOrderId": f"ORD-{order.id}",
        "merchantTimestamp": datetime.now(timezone.utc).isoformat(),
        "successUrl": f"http://localhost:5000/payment/success/{order.id}",
        "failedUrl": f"http://localhost:5000/payment/failed/{order.id}",
        "errorUrl": f"http://localhost:5000/payment/error/{order.id}",
    }

    try:
        r = requests.post(f"{PSP_BASE_URL}/api/payments/init", json=payload, verify=False, timeout=10)
        r.raise_for_status()
        data = r.json()
    except Exception as ex:
        order.status = "FAILED"
        db.session.commit()
        return f"PSP init failed: {ex}", 500

    payment_id = data["paymentId"]

    # 3) preusmeri na PSP stranicu 
    return redirect(f"{PSP_BASE_URL}/pay/{payment_id}")

@app.route("/payment/success/<int:order_id>")
def payment_success(order_id: int):
    if not require_login():
        return redirect(url_for("login"))

    o = Order.query.get_or_404(order_id)
    if o.user_id != session["user_id"]:
        return "Forbidden", 403

    o.status = "SUCCESS"
    o.payment_method = "CARD"
    db.session.commit()
    return render_template("payment_result.html", status="SUCCESS", order=o)

@app.route("/payment/failed/<int:order_id>")
def payment_failed(order_id: int):
    if not require_login():
        return redirect(url_for("login"))

    o = Order.query.get_or_404(order_id)
    if o.user_id != session["user_id"]:
        return "Forbidden", 403

    o.status = "FAILED"
    o.payment_method = "CARD"
    db.session.commit()
    return render_template("payment_result.html", status="FAILED", order=o)

@app.route("/payment/error/<int:order_id>")
def payment_error(order_id: int):
    if not require_login():
        return redirect(url_for("login"))

    o = Order.query.get_or_404(order_id)
    if o.user_id != session["user_id"]:
        return "Forbidden", 403

    o.status = "FAILED"
    o.payment_method = "CARD"
    db.session.commit()
    return render_template("payment_result.html", status="ERROR", order=o)



def seed_packages_if_empty():
    if Package.query.count() == 0:
        db.session.add_all([
            Package(name="Economy paket", price=15000, currency="RSD", is_active=True),
            Package(name="Standard paket", price=22000, currency="RSD", is_active=True),
            Package(name="Premium paket", price=35000, currency="RSD", is_active=True),
        ])
        db.session.commit()

def require_admin():
    return session.get("is_admin") is True

if __name__ == "__main__":
    with app.app_context():
        db.create_all()  # kreira tabele ako ne postoje
        seed_packages_if_empty()

    app.run(port=5000, debug=True)
