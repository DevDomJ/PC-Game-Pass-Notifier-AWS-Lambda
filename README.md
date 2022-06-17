# PC Game Pass Notifier AWS Lambda
This is a small project to automatically notify me via Discord whenever games are added to or removed from Microsoft's PC Game Pass.

The project makes use of AWS Lambda to run the code, CloudWatch to monitor the execution logs and to schedule the update to run every six hours, and S3 to save a JSON file with the last state.

It uses the Discord API to send messages to a certain server and channel via a Discord webhook.

It uses some kind of xbox game catalog which seems to be used by the xbox game store as well, to retrieve the game ids of the games currently in the game pass and to get detailed information (like title, description & art) about those games.

The CloudWatch scheduled event which invokes the lamdba function, passes the required parameters like the Discord webhook url, pc game pass catalog url, AWS bucket name etc. as JSON to the lamdba function. This makes the project theoretically usable by others, while keeping this sensitive information out of the code (and repo).

However, this is more of a private project for my own fun and portfolio, so I probably won't include information on how to setup this project for somebody else.