# 📋 Test Cases

| Test ID | Feature | Test Data (Input) | Expected Result |
|--------|--------|------------------|----------------|
| TC1 | Apply Job (Low Score) | Score = 40, Tier = Startup | Application rejected |
| TC2 | Apply Job (Valid Score) | Score = 80, Tier = Mid-tier | Interview or success |
| TC3 | Apply Job (Big Tech) | Score = 90 + requirements met | Offer / employment |
| TC4 | Game Over Condition | 3 failed applications | Game over triggered |
| TC5 | Activity Completion | Complete project | Score increases |
| TC6 | Tier Restriction | Apply to mid-tier without startup | Blocked |
| TC7 | Networking Bonus | Increase networking count | Resume multiplier increases |
| TC8 | Resume Tailoring | Complete resume activity | Resume marked as improved |
| TC9 | Invalid Input | Missing player_id | Error returned |
| TC10 | Repeated Activity | Same activity twice | No duplicate reward |
