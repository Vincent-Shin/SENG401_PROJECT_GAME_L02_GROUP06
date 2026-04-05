# Test Cases

| Test ID | Feature | Test Level | Test Data (Input) | Expected Result |
|--------|--------|------------|------------------|----------------|
| TC1 | Apply Job (Low Score) | Unit | Score = 40, Tier = Startup | Application rejected |
| TC2 | Apply Job (Valid Score) | Unit | Score = 80, Tier = Mid-tier | Interview or success |
| TC3 | Apply Job (Big Tech) | System | Score = 90 + requirements met | Offer / employment |
| TC4 | Game Over Condition | System | 3 failed applications | Game over triggered |
| TC5 | Activity Completion | Unit | Complete project | Score increases |
| TC6 | Tier Restriction | Unit | Apply without requirement | Blocked |
| TC7 | Networking Bonus | Unit | Increase networking count | Multiplier increases |
| TC8 | Resume Tailoring | Integration | Complete activity via frontend | Resume updated |
| TC9 | Invalid Input | Integration | Missing player_id | Error returned |
| TC10 | Repeated Activity | System | Repeat same action | No duplicate reward |
