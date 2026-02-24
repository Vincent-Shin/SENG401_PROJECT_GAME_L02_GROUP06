# SENG401_PROJECT_GAME_L02_GROUP06

## SENG 401 – Software Architecture  
### Serious Game Project  
**Unemployed Simulator**  
Group 06 – L02  

---

## 👥 Group Members

- Mai, Trung Tuan  
- Ndabaramiye, Benny  
- Sekhon, Manjot  
- VUONG, Justin  

---

## 🎮 Project Overview

**Unemployed Simulator** is a 2D serious digital game developed using Unity.  
The game addresses **United Nations Sustainable Development Goal 8: Decent Work and Economic Growth**.

The objective of the game is to simulate the experience of a software engineering graduate seeking their first job or internship. Players improve their resume score by completing mini-projects, earning certificates, networking, and gaining experience. Market conditions and company tiers influence the probability of receiving interview call-backs and job offers.

The game promotes awareness of:
- Labor market competition
- Strategic skill development
- Resume optimization
- Networking importance
- Market uncertainty

---

## 🏗 Architecture

This project follows a **Layered Architecture**:

### 1. Presentation Layer
- Unity 2D (C#)
- Handles player input, UI, game logic visualization

### 2. Application Layer
- Backend API (Node.js / Flask)
- Resume scoring system
- Interview probability calculation
- Market condition simulation

### 3. Data Layer
- SQL Database (PostgreSQL / MySQL)
- Stores:
  - Player profiles
  - Resume scores
  - Application history
  - Market states
  - Mini-game results

Flow Direction:
Unity → Backend API → SQL Database

---

## ⚙️ Core Gameplay Systems

- Resume Scoring System
- Company Tier Classification (Startup / Mid-tier / Big Tech)
- Market Condition Phases
- Randomized Interview Probability Model
- Mini-games for skill development
- Success and failure endings

---

## 🛠 Tech Stack

- Unity 2D (C#)
- Backend: Node.js or Flask
- Database: PostgreSQL / MySQL
- GitHub for version control

---

## 📌 Current Progress

- Map design completed
- Player movement and collision implemented
- Initial resume scoring system drafted
- Company tier logic defined
- Market simulation logic drafted

---

## 🚀 Future Work

- Backend API implementation
- SQL database schema integration
- Unity–Backend integration
- UI refinement
- Testing and validation

---

## 📄 License

Academic project for SENG 401 – Winter 2026.
