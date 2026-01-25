#!/bin/bash
# Development helper script for starting Aspire with proper configuration
# This script handles certificate issues on macOS and other development environments

set -e

# Source development environment variables if they exist
if [ -f .env.development ]; then
  export $(grep -v '^#' .env.development | xargs)
fi

# Start Aspire
aspire run "$@"
