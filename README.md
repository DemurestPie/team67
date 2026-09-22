# team67

# BucHunt

## Project Overview

BucHunt is a browser-based scavenger hunt application based on the ScavengeRUS project. The system allows organizations and businesses to create scavenger hunt experiences where players travel to different locations and complete tasks.

Players can participate in a hunt, view their tasks, travel to task locations, and complete tasks using methods such as QR codes and GPS location. The system keeps track of player progress and allows players to compare their progress with others.

The goal of BucHunt is to provide a scavenger hunt system that works across phones, tablets, and computers without requiring users to install a separate application.

## Main Users

### Player
A player participates in a scavenger hunt. Players can:

- Register for a hunt
- Join and start a hunt
- View available tasks
- View task locations on a map
- Scan QR codes
- Complete tasks
- See finished and unfinished tasks
- View their progress
- Compare their progress with other players
- Participate as part of a team

### Admin
An Admin manages the scavenger hunt and its information. Admin functionality will include managing hunts, tasks, locations, and player participation.

### Businesses
Businesses can participate by having locations associated with the scavenger hunt.

## Initial Requirements

The current requirements identified during initial grooming include:

1. Businesses need a way to add their place.
2. A digital sign-up form must allow players to register for a hunt.
3. The map must update when new locations are added.
4. Players must see unfinished tasks before finished tasks.
5. QR codes must be unique for each hunt.
6. QR codes can be used to advertise or identify locations.
7. The application must have a landing page.
8. Players can finish and mark off tasks.
9. Players can start a task.
10. Users can create an account.
11. Players can see how their progress compares with other players.
12. Players can enter an access code to join a hunt.
13. The map must provide players with directions to task locations.
14. Players can scan a QR code to complete a task.
15. The system tells players whether a task submission is valid or invalid.
16. Team progress automatically updates across different devices.
17. Players receive a congratulations message after completing all tasks.
18. GPS can be used to determine whether a player is near the required task location.
19. The map shows scavenger hunt task locations.
20. The system records completed tasks and their completion time.

## Basic Hunt Flow

1. A player registers for a scavenger hunt.
2. The player accesses the BucHunt website.
3. The player joins the appropriate hunt.
4. The player views available tasks and locations.
5. The player travels to a task location.
6. The player completes the task using the required method, such as scanning a QR code.
7. BucHunt validates the submission.
8. A valid task is marked as completed.
9. Player and team progress is updated.
10. The player continues until all required tasks are completed.
11. The player receives a congratulations message when the hunt is completed.

## Technical Requirements

BucHunt will use a client-server architecture. The client interface will be browser-based and should work across different devices, including phones, tablets, and computers.

Player progress and application data should be stored on the server rather than depending on browser cookies. The system will also need to account for security, logging, and reliable tracking of task completion.

## Project Status

The team is currently reviewing the project requirements, identifying major features, clarifying unclear requirements, and preparing the product backlog.

Requirements may be changed, clarified, split, or expanded as the project moves through grooming and development.

## Future Work

Future development may include:

- Additional task types
- Trivia questions
- Different task ordering options
- Improved hunt management
- Player invitations and notifications
- Additional map and GPS features
- Hunt customization
- Business sponsorship features
- Additional administrative functionality

## Team

BucHunt Development Team
