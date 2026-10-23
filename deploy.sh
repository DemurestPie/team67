#!/bin/bash

# ==========================
# Team 67 Deployment Script
# CSCI 4350-002 Fall 2026
# ==========================

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Exit immediately if a command exits with a non-zero status
set -e

REPO_URL="https://github.com/DemurestPie/team67.git"

# Change directory to the repository if not already in it
if [ -d "team67" ]; then
	echo -e "${YELLOW}Warning: The script is not running inside the 'team67' directory. Changing directory to 'team67'.${NC}"
	cd team67 || { echo -e "${RED}Failed to change directory to team67${NC}"; exit 1; }
fi

# Check if .NET 9 is installed. If not, ask user wants to install it. If yes, install it. If no, exit the script.
if ! command -v dotnet &> /dev/null || ! dotnet --list-sdks | grep -q "9\."; then
	echo -e "${RED}Error: .NET 9 is not installed.${NC}"
	read -p "Do you want to install .NET 9? (y/n): " INSTALL_DOTNET
	if [[ "$INSTALL_DOTNET" == "y" || "$INSTALL_DOTNET" == "Y" || -z "$INSTALL_DOTNET" ]]; then
		echo -e "${YELLOW}Installing .NET 9...${NC}"
		sudo apt-get update
		sudo add-apt-repository -y ppa:dotnet/backports
		# Update packages to prevent dependency issues
		echo -e "${YELLOW}Updating packages...${NC}"
		sudo apt-get upgrade -y
		sudo apt-get install -y dotnet-sdk-9.0
		echo -e "${GREEN}.NET 9 installed successfully.${NC}"
	else
		echo -e "${RED}Please install .NET 9 and re-run the script.${NC}"
		exit 1
	fi
fi

# Check if git is installed. If not, ask user wants to install it. If yes, install it. If no, exit the script.
if ! command -v git &> /dev/null; then
	echo -e "${RED}Error: git is not installed.${NC}"
	read -p "Do you want to install git? (y/n): " INSTALL_GIT
	if [[ "$INSTALL_GIT" == "y" || "$INSTALL_GIT" == "Y" || -z "$INSTALL_GIT" ]]; then
		echo -e "${YELLOW}Installing git...${NC}"
		sudo apt-get update
		sudo apt-get install -y git
		echo -e "${GREEN}git installed successfully.${NC}"
	else
		echo -e "${RED}Please install git and re-run the script.${NC}"
		exit 1
	fi
fi

# Prompt user for branch
if [ -z "$1"  ]; then
	read -p "Enter the branch name to deploy (CASE SENSITIVE): " BRANCH
else
	BRANCH="$1"
fi

# Check if branch name is empty
if [ -z "$BRANCH" ]; then
	echo -e "${RED}Error: Branch name cannot be empty.${NC}"
	exit 1
fi

echo -e "${GREEN}=============================${NC}"
echo -e "${GREEN} Deploying branch: $BRANCH   ${NC}"
echo -e "${GREEN}=============================${NC}"

# Make sure the git repo is initialized
if [ ! -d ".git" ]; then
	echo -e "${YELLOW}Initializing git repository...${NC}"
	git init
	git remote add origin "$REPO_URL" || git remote set-url origin "$REPO_URL"
fi

# Pull latest code from github
echo -e "${YELLOW}Fetching latest changes...${NC}"
git fetch origin "$BRANCH"
git checkout "$BRANCH"
git pull origin "$BRANCH"

# Build the app
echo -e "${YELLOW}Building app...${NC}"
dotnet build --configuration Release

echo -e "${GREEN}==============================${NC}"
echo -e "${GREEN} $BRANCH build completed! ${NC}"
echo -e "${GREEN}==============================${NC}"

# Run the app on localhost:5000
echo -e "${YELLOW}Starting application on localhost:5000${NC}"
dotnet run --configuration Release --urls "http://0.0.0.0:5000"
