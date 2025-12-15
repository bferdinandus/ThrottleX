#!/bin/bash

# Define constants
PROJECT_DIR="./src/ThrottleX.Core"
PROJECT_NAME="ThrottleX.Core"
RID="linux-arm64" # <-- Adjust this to linux-arm if you are using a 32-bit OS
PUBLISH_DIR="publish_pi_temp"
PI_SETTINGS="appsettings.Pi.json"


# --- 1. Validate Command Line Arguments ---
if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <pi_username> <raspberry_pi_ip_address>"
    echo "Example: $0 pi 10.2.0.2"
    exit 1
fi

PI_USER=$1
PI_IP=$2
# This is the EXACT final location where the app should live on the Pi
PI_DEST_PATH="/home/$PI_USER/apps/$PROJECT_NAME"


# --- 2. Run the Publish Command ---
echo "--- Publishing project $PROJECT_NAME for $RID ---"
dotnet publish "$PROJECT_DIR/$PROJECT_NAME.csproj" --configuration Release --runtime "$RID" --self-contained true --output "./$PUBLISH_DIR"

if [ $? -ne 0 ]; then
    echo "ERROR: dotnet publish failed."
    exit 1
fi


# --- 3. Copy the Pi-specific settings file (optional) ---
echo "--- Copying $PI_SETTINGS file into publish folder ---"
if [ -f "$PI_SETTINGS" ]; then
    cp "$PI_SETTINGS" "./$PUBLISH_DIR/"
else
    echo "Warning: $PI_SETTINGS not found in the script's root directory. Skipping copy."
fi

# --- Remove appsettings.Development.json from publish directory ---
if [ -f "./$PUBLISH_DIR/appsettings.Development.json" ]; then
    rm "./$PUBLISH_DIR/appsettings.Development.json"
    echo "Removed appsettings.Development.json from the publish directory."
fi

# --- 4. Transfer the entire directory via SCP ---

echo "--- Transferring directory $PUBLISH_DIR to $PI_IP:$PI_DEST_PATH ---"

# 4a. Use SSH to create the destination directory and clear any old files first
ssh "$PI_USER@$PI_IP" "mkdir -p $PI_DEST_PATH && rm -rf $PI_DEST_PATH/*"

# 4b. Use SCP to transfer the contents of the local temporary folder into the remote destination folder
# Note the trailing slash '/' on the local path to copy contents, not the folder itself recursively.
scp -r "./$PUBLISH_DIR/." "$PI_USER@$PI_IP:$PI_DEST_PATH/"


# --- 5. Clean up local publish folder ---
echo "--- Cleaning up local temporary folder ---"
rm -rf "./$PUBLISH_DIR"

echo " "
echo "--- Deployment script finished. ---"
echo "You can now SSH into your Pi and run the application: "
echo "ssh $PI_USER@$PI_IP"
echo "cd $PI_DEST_PATH"
echo "./$PROJECT_NAME"
