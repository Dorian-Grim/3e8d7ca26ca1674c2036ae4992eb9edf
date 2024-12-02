# Demo

![demo](https://github.com/user-attachments/assets/34ee4254-b389-4906-b3f0-c994a94d5717)

# Architecture diagram

![image](https://github.com/user-attachments/assets/e5e03582-56c9-46b5-8822-5b6cd11e1912)

# ERD diagram

![image](https://github.com/user-attachments/assets/8c4d3844-8584-4fc3-8de0-a01c0f63dc67)

# DefenseAssetManagement

The solution is working as it is, see demo bellow. When building 3 projects are setup to start:

1. the websocket server (comms)
2. the command center UI (command)
3. the soldier registration UI (base)

The command center is based in Bucharest, Piața Unirii. It connects through a websocket server to the soldier registration base. There soldiers come and register, the websocket server sends the intent back to the base and saves the soldier to the database. The command center can then deploy the soldier from the base starting at Lat 0, Lon 0, towards the North East (hardcoded). 

Currently the soldiers move every second with 2 meters/s for a duration of 60 seconds. I have tested with 1ms move interval and works ok.
![demo2](https://github.com/user-attachments/assets/f7294463-9b89-4d39-aebe-c6a38d50dff4)

# Prerequisite

Publish the database project from the solution to local MSSQL server DAM db before running the app.

## TODO

1. MVVC design
2. Unit tests
3. End-to-end integration test
