[![.NET Build & Test](https://github.com/DevDomJ/PC-Game-Pass-Notifier-AWS-Lambda/actions/workflows/dotnet.yml/badge.svg)](https://github.com/DevDomJ/PC-Game-Pass-Notifier-AWS-Lambda/actions/workflows/dotnet.yml)

# PC Game Pass Notifier AWS Lambda
This is a small project to automatically notify me via Discord whenever games are added to or removed from Microsoft's PC Game Pass.

The project makes use of AWS Lambda to run the code, CloudWatch to monitor the execution logs and to schedule the update to run every six hours, and S3 to save a JSON file with the last state.

It uses the Discord API to send messages to a certain server and channel via a Discord webhook.

It uses some kind of xbox game catalog which seems to be used by the xbox game store as well, to retrieve the game ids of the games currently in the game pass and to get detailed information (like title, description & art) about those games.

The CloudWatch scheduled event which invokes the lamdba function, passes the required parameters like the Discord webhook url, pc game pass catalog url, AWS bucket name etc. as JSON to the lamdba function. This makes the project theoretically usable by others, while keeping this sensitive information out of the code (and repo).

However, this is more of a private project for my own fun and portfolio, so I probably won't include information on how to setup this project for somebody else.


## Setup

To set up this project for local development and testing, follow these steps:

1. **Clone the Repository:**
   ```sh
   git clone https://github.com/DevDomJ/PC-Game-Pass-Notifier-AWS-Lambda.git
   cd PC-Game-Pass-Notifier-AWS-Lambda
   ```

2. **Create an `.env` File:**

   	In the root directory of your project, create a file named .env. Add your environment variables to this file in the following format:
  
    ```plaintext
    DISCORD_WEBHOOK_URL=https://discord.com/api/webhooks/your-webhook-url
    ```

4.  **Build the project:**

	Build your project to ensure all dependencies are correctly installed:
	
	  ```sh
	  dotnet build
	  ```

6.  **Run the tests:**

	Run the tests to verify everything is set up correctly:
	  
	  ```sh
	  dotnet test
	  ```

By following these steps, you should be able to set up and run the project on your local machine.

## Notes:
- The .env file is excluded from the repository and should not be committed. Ensure you have the necessary environment variables configured in your local .env file for the project to work correctly.
- For production and CI/CD environments, such as AWS Lambda and GitHub Actions, configure the environment variables directly in the respective platforms.
