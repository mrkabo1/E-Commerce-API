# 🛒 E-Commerce API (ASP.NET Core)

A production-ready E-Commerce RESTful API built using ASP.NET Core with Clean Architecture principles.
The project simulates a real-world backend system with authentication, payments, and scalable design.

---

## 🚀 Live Demo

🔗 API Base URL:
https://e-commerce-api.tryasp.net

🔗 Swagger Documentation:
https://e-commerce-api.tryasp.net/swagger

---

## 🧠 Features

* 🔐 Authentication & Authorization (JWT)
* 👤 User Management
* 🛒 Shopping Cart System
* 📦 Orders Management
* 💳 Payment Integration using Stripe
* 📄 Pagination & Filtering
* ⚙️ Clean Architecture Implementation
* 🧩 DTOs & AutoMapper
* 🛡️ Global Exception Handling
* 📝 Logging System
* 📘 Swagger API Documentation

---

## 🏗️ Architecture

This project follows **Clean Architecture**:

* Domain Layer
* Application Layer
* Infrastructure Layer
* Presentation Layer (API)

---

## 🛠️ Technologies Used

* ASP.NET Core Web API
* Entity Framework Core
* SQL Server
* JWT Authentication
* Stripe Payment Gateway
* AutoMapper
* Serilog (Logging)
* Swagger

---

## 📦 API Endpoints Overview

### Auth

* POST /api/auth/register
* POST /api/auth/login

### Products

* GET /api/products
* GET /api/products/{id}

### Cart

* GET /api/cart
* POST /api/cart

### Orders

* POST /api/orders
* GET /api/orders

### Payments

* POST /api/payments

---

## ⚙️ Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/mrkabo1/E-Commerce-API.git
```

### 2. Navigate to project

```bash
cd E-Commerce-API
```

### 3. Configure appsettings.json

* Add your database connection string
* Add Stripe keys

### 4. Apply migrations

```bash
dotnet ef database update
```

### 5. Run the project

```bash
dotnet run
```

---

## 🔑 Environment Variables

* JWT Secret Key
* Stripe Secret Key
* Database Connection String

---

## 📌 Notes

* This project is built as a real-world simulation of an E-commerce backend.
* Payment integration uses Stripe test mode.
* Designed with scalability and maintainability in mind.

---

## 👨‍💻 Author

**Youssef Mohamed Gomaa**
Backend .NET Developer

---

## 📬 Contact

* GitHub: https://github.com/mrkabo1
* Email: yosefkabo303@gmail.com
