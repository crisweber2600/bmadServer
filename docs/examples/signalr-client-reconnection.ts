/**
 * SignalR Client with Automatic Reconnection
 * 
 * This example demonstrates how to implement a SignalR client with:
 * - Automatic reconnection with exponential backoff (0s, 2s, 10s, 30s)
 * - JWT authentication via query string
 * - Session recovery after reconnection
 * - Connection state management
 * 
 * Usage:
 *   npm install @microsoft/signalr
 *   
 *   const client = new SignalRChatClient('https://api.bmadserver.dev', getAccessToken);
 *   await client.connect();
 *   await client.sendMessage('Hello, world!');
 */

import * as signalR from '@microsoft/signalr';

export interface ChatMessage {
  role: string;
  content: string;
  timestamp: Date;
}

export interface SessionRestored {
  id: string;
  workflowName: string;
  currentStep: string;
  conversationHistory: ChatMessage[];
  pendingInput: string | null;
  message: string;
}

export class SignalRChatClient {
  private connection: signalR.HubConnection;
  private accessTokenProvider: () => Promise<string>;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 10;

  /**
   * Creates a new SignalR chat client
   * @param hubUrl - The SignalR hub URL (e.g., 'https://api.bmadserver.dev/hubs/chat')
   * @param accessTokenProvider - Function that returns the current JWT access token
   */
  constructor(hubUrl: string, accessTokenProvider: () => Promise<string>) {
    this.accessTokenProvider = accessTokenProvider;

    // Configure connection with automatic reconnection
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: async () => await this.accessTokenProvider()
      })
      .withAutomaticReconnect({
        // Custom retry policy: 0s, 2s, 10s, 30s, then 30s repeatedly
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount >= this.maxReconnectAttempts) {
            // Stop reconnecting after max attempts
            return null;
          }
          
          this.reconnectAttempts = retryContext.previousRetryCount;
          
          // Exponential backoff: 0, 2000, 10000, 30000, 30000...
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount === 1) return 2000;
          if (retryContext.previousRetryCount === 2) return 10000;
          return 30000;
        }
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();
  }

  /**
   * Set up connection event handlers
   */
  private setupEventHandlers(): void {
    // Handle reconnecting state
    this.connection.onreconnecting((error) => {
      console.warn(`Connection lost. Reconnecting... (Attempt ${this.reconnectAttempts + 1})`, error);
      this.onReconnecting?.(this.reconnectAttempts + 1, error);
    });

    // Handle successful reconnection
    this.connection.onreconnected((connectionId) => {
      console.log(`Reconnected successfully with connection ID: ${connectionId}`);
      this.reconnectAttempts = 0;
      this.onReconnected?.(connectionId);
    });

    // Handle connection closed
    this.connection.onclose((error) => {
      console.error('Connection closed', error);
      this.onDisconnected?.(error);
      
      // Attempt manual reconnection if automatic reconnection failed
      if (this.reconnectAttempts >= this.maxReconnectAttempts) {
        console.error('Max reconnection attempts reached. Please refresh the page.');
        this.onMaxReconnectAttemptsReached?.();
      }
    });
  }

  /**
   * Connect to the SignalR hub
   */
  async connect(): Promise<void> {
    try {
      await this.connection.start();
      console.log(`Connected to SignalR hub with connection ID: ${this.connection.connectionId}`);
      this.onConnected?.(this.connection.connectionId!);
    } catch (error) {
      console.error('Failed to connect to SignalR hub', error);
      throw error;
    }
  }

  /**
   * Disconnect from the SignalR hub
   */
  async disconnect(): Promise<void> {
    await this.connection.stop();
  }

  /**
   * Send a chat message to the hub
   */
  async sendMessage(message: string): Promise<void> {
    if (this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Cannot send message: Not connected to hub');
    }
    
    await this.connection.invoke('SendMessage', message);
  }

  /**
   * Join a workflow group for targeted messaging
   */
  async joinWorkflow(workflowName: string): Promise<void> {
    await this.connection.invoke('JoinWorkflow', workflowName);
  }

  /**
   * Leave a workflow group
   */
  async leaveWorkflow(workflowName: string): Promise<void> {
    await this.connection.invoke('LeaveWorkflow', workflowName);
  }

  /**
   * Register handler for incoming messages
   */
  onMessage(handler: (message: ChatMessage) => void): void {
    this.connection.on('ReceiveMessage', handler);
  }

  /**
   * Register handler for session restoration
   */
  onSessionRestored(handler: (session: SessionRestored) => void): void {
    this.connection.on('SESSION_RESTORED', handler);
  }

  // Event callbacks (can be overridden or set by consumers)
  onConnected?: (connectionId: string) => void;
  onReconnecting?: (attempt: number, error?: Error) => void;
  onReconnected?: (connectionId: string) => void;
  onDisconnected?: (error?: Error) => void;
  onMaxReconnectAttemptsReached?: () => void;
}

// Example usage
async function example() {
  // Function to get access token (e.g., from localStorage or auth service)
  const getAccessToken = async (): Promise<string> => {
    // Replace with your actual token retrieval logic
    return localStorage.getItem('accessToken') || '';
  };

  // Create client
  const client = new SignalRChatClient(
    'https://api.bmadserver.dev/hubs/chat',
    getAccessToken
  );

  // Set up event handlers
  client.onConnected = (connectionId) => {
    console.log(`✓ Connected with ID: ${connectionId}`);
  };

  client.onReconnecting = (attempt, error) => {
    console.log(`⟳ Reconnecting (attempt ${attempt})...`);
    // Show reconnecting UI indicator
  };

  client.onReconnected = (connectionId) => {
    console.log(`✓ Reconnected with ID: ${connectionId}`);
    // Hide reconnecting UI indicator
  };

  client.onDisconnected = (error) => {
    console.error('✗ Disconnected:', error);
    // Show disconnected UI state
  };

  client.onMaxReconnectAttemptsReached = () => {
    console.error('✗ Max reconnection attempts reached. Please refresh the page.');
    // Show error message to user
  };

  // Register message handler
  client.onMessage((message) => {
    console.log(`Received: [${message.role}] ${message.content}`);
    // Update UI with new message
  });

  // Register session restoration handler
  client.onSessionRestored((session) => {
    console.log(`Session restored: ${session.message}`);
    console.log(`Workflow: ${session.workflowName}, Step: ${session.currentStep}`);
    // Restore conversation history in UI
    session.conversationHistory.forEach(msg => {
      console.log(`  [${msg.role}] ${msg.content}`);
    });
  });

  // Connect to hub
  await client.connect();

  // Join a workflow
  await client.joinWorkflow('onboarding');

  // Send a message
  await client.sendMessage('Hello, I need help with onboarding');

  // When done, disconnect
  // await client.disconnect();
}
