import { useState, useCallback, useRef, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';

/**
 * Represents an agent handoff event from the backend
 */
export interface AgentHandoffEvent {
  FromAgentId: string;
  FromAgentName: string;
  ToAgentId: string;
  ToAgentName: string;
  StepName?: string;
  Reason?: string;
  Timestamp: Date;
  FromAvatarUrl?: string;
  ToAvatarUrl?: string;
}

/**
 * Represents a user online/offline event
 */
export interface UserPresenceEvent {
  UserId: string;
  DisplayName: string;
  IsOnline: boolean;
  LastSeen?: Date;
}

/**
 * Represents a user typing event
 */
export interface UserTypingEvent {
  UserId: string;
  DisplayName: string;
  WorkflowId?: string;
}

/**
 * Hook options for SignalR handoff handling
 */
export interface UseSignalRHandoffsOptions {
  /** Callback when a handoff event is received */
  onHandoff?: (event: AgentHandoffEvent) => void;
  /** Callback when connection state changes */
  onConnectionStateChange?: (state: 'connected' | 'reconnecting' | 'disconnected') => void;
  /** Callback when a user comes online */
  onUserOnline?: (event: UserPresenceEvent) => void;
  /** Callback when a user goes offline */
  onUserOffline?: (event: UserPresenceEvent) => void;
  /** Callback when a user is typing */
  onUserTyping?: (event: UserTypingEvent) => void;
  /** Enable debug logging */
  debug?: boolean;
  /** Typing timeout in milliseconds - default 3000 */
  typingTimeoutMs?: number;
}

/**
 * Hook for handling SignalR AGENT_HANDOFF events
 * 
 * Manages the SignalR connection and listens for agent handoff events.
 * Automatically handles reconnection with exponential backoff.
 * 
 * @example
 * ```tsx
 * const { handoffHistory, connectionState, error } = useSignalRHandoffs({
 *   onHandoff: (event) => {
 *     console.log(`Agent handoff: ${event.FromAgentName} -> ${event.ToAgentName}`);
 *     // Insert AgentHandoffIndicator into chat
 *   }
 * });
 * ```
 */
export function useSignalRHandoffs(options?: UseSignalRHandoffsOptions) {
  const { 
    onHandoff, 
    onConnectionStateChange, 
    onUserOnline,
    onUserOffline,
    onUserTyping,
    debug = false,
    typingTimeoutMs = 3000,
  } = options || {};
  
  // Connection state
  const [connectionState, setConnectionState] = useState<'connected' | 'reconnecting' | 'disconnected'>('disconnected');
  const [error, setError] = useState<string | null>(null);
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const reconnectAttemptsRef = useRef(0);
  const maxReconnectAttemptsRef = useRef(10);
  
  // Handoff history
  const [handoffHistory, setHandoffHistory] = useState<AgentHandoffEvent[]>([]);
  
  // Presence tracking
  const [onlineUsers, setOnlineUsers] = useState<UserPresenceEvent[]>([]);
  const [typingUsers, setTypingUsers] = useState<string[]>([]);
  const typingTimeoutsRef = useRef<Map<string, NodeJS.Timeout>>(new Map());

  // Debug logging helper
  const log = useCallback((message: string, data?: unknown) => {
    if (debug) {
      console.log(`[SignalR Handoffs] ${message}`, data || '');
    }
  }, [debug]);

  // Initialize and connect to SignalR hub
  useEffect(() => {
    const initializeConnection = async () => {
      try {
        log('Initializing SignalR connection...');
        
        // Get the API base URL (could be from env or config)
        const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:8080';
        const hubUrl = `${apiUrl}/hubs/chat`;
        
        // Build connection with automatic reconnection
        const connection = new signalR.HubConnectionBuilder()
          .withUrl(hubUrl, {
            accessTokenFactory: async () => {
              // Get JWT token from localStorage (or your auth service)
              return localStorage.getItem('accessToken') || '';
            }
          })
          .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: (retryContext) => {
              if (retryContext.previousRetryCount >= maxReconnectAttemptsRef.current) {
                return null; // Stop reconnecting
              }
              
              reconnectAttemptsRef.current = retryContext.previousRetryCount;
              
              // Exponential backoff: 0, 2000, 10000, 30000, 30000...
              if (retryContext.previousRetryCount === 0) return 0;
              if (retryContext.previousRetryCount === 1) return 2000;
              if (retryContext.previousRetryCount === 2) return 10000;
              return 30000;
            }
          })
          .configureLogging(debug ? signalR.LogLevel.Information : signalR.LogLevel.Error)
          .build();

        // Set up connection event handlers
        connection.onreconnecting((error) => {
          log(`Connection lost. Reconnecting (Attempt ${reconnectAttemptsRef.current + 1})`, error);
          setConnectionState('reconnecting');
          onConnectionStateChange?.('reconnecting');
        });

        connection.onreconnected((connectionId) => {
          log(`Reconnected successfully with connection ID: ${connectionId}`);
          setConnectionState('connected');
          onConnectionStateChange?.('connected');
          setError(null);
          reconnectAttemptsRef.current = 0;
        });

        connection.onclose((error) => {
          log('Connection closed', error);
          setConnectionState('disconnected');
          onConnectionStateChange?.('disconnected');
          
          if (reconnectAttemptsRef.current >= maxReconnectAttemptsRef.current) {
            const errorMsg = 'Max reconnection attempts reached. Please refresh the page.';
            setError(errorMsg);
            console.error(errorMsg);
          }
        });

        // Register handler for AGENT_HANDOFF events
        connection.on('AGENT_HANDOFF', (payload: {
          FromAgentId: string;
          FromAgentName: string;
          ToAgentId: string;
          ToAgentName: string;
          StepName?: string;
          Reason?: string;
          Timestamp: string;
          FromAvatarUrl?: string;
          ToAvatarUrl?: string;
        }) => {
          log('AGENT_HANDOFF event received', payload);
          
          const handoffEvent: AgentHandoffEvent = {
            FromAgentId: payload.FromAgentId,
            FromAgentName: payload.FromAgentName,
            ToAgentId: payload.ToAgentId,
            ToAgentName: payload.ToAgentName,
            StepName: payload.StepName,
            Reason: payload.Reason,
            Timestamp: new Date(payload.Timestamp),
            FromAvatarUrl: payload.FromAvatarUrl,
            ToAvatarUrl: payload.ToAvatarUrl,
          };
          
          // Add to history
          setHandoffHistory((prev) => [...prev, handoffEvent]);
          
          // Call consumer callback
          onHandoff?.(handoffEvent);
        });

        // Register handler for USER_ONLINE events
        connection.on('USER_ONLINE', (payload: {
          UserId: string;
          DisplayName: string;
          IsOnline?: boolean;
          LastSeen?: string;
        }) => {
          log('USER_ONLINE event received', payload);
          
          const presenceEvent: UserPresenceEvent = {
            UserId: payload.UserId,
            DisplayName: payload.DisplayName,
            IsOnline: payload.IsOnline ?? true,
            LastSeen: payload.LastSeen ? new Date(payload.LastSeen) : undefined,
          };
          
          // Add or update user in online list
          setOnlineUsers((prev) => {
            const existing = prev.findIndex(u => u.UserId === presenceEvent.UserId);
            if (existing >= 0) {
              const updated = [...prev];
              updated[existing] = presenceEvent;
              return updated;
            }
            return [...prev, presenceEvent];
          });
          
          // Call consumer callback
          onUserOnline?.(presenceEvent);
        });

        // Register handler for USER_OFFLINE events
        connection.on('USER_OFFLINE', (payload: {
          UserId: string;
          DisplayName: string;
          IsOnline?: boolean;
          LastSeen?: string;
        }) => {
          log('USER_OFFLINE event received', payload);
          
          const presenceEvent: UserPresenceEvent = {
            UserId: payload.UserId,
            DisplayName: payload.DisplayName,
            IsOnline: false,
            LastSeen: payload.LastSeen ? new Date(payload.LastSeen) : new Date(),
          };
          
          // Remove user from online list
          setOnlineUsers((prev) => prev.filter(u => u.UserId !== payload.UserId));
          
          // Also remove from typing users
          setTypingUsers((prev) => prev.filter(name => name !== payload.DisplayName));
          
          // Clear any typing timeout for this user
          const existingTimeout = typingTimeoutsRef.current.get(payload.DisplayName);
          if (existingTimeout) {
            clearTimeout(existingTimeout);
            typingTimeoutsRef.current.delete(payload.DisplayName);
          }
          
          // Call consumer callback
          onUserOffline?.(presenceEvent);
        });

        // Register handler for USER_TYPING events
        connection.on('USER_TYPING', (payload: {
          UserId: string;
          DisplayName: string;
          WorkflowId?: string;
        }) => {
          log('USER_TYPING event received', payload);
          
          const typingEvent: UserTypingEvent = {
            UserId: payload.UserId,
            DisplayName: payload.DisplayName,
            WorkflowId: payload.WorkflowId,
          };
          
          // Add to typing users if not already present
          setTypingUsers((prev) => {
            if (prev.includes(payload.DisplayName)) {
              return prev;
            }
            return [...prev, payload.DisplayName];
          });
          
          // Clear existing timeout for this user
          const existingTimeout = typingTimeoutsRef.current.get(payload.DisplayName);
          if (existingTimeout) {
            clearTimeout(existingTimeout);
          }
          
          // Set timeout to remove from typing list
          const timeoutId = setTimeout(() => {
            setTypingUsers((prev) => prev.filter(name => name !== payload.DisplayName));
            typingTimeoutsRef.current.delete(payload.DisplayName);
          }, typingTimeoutMs);
          
          typingTimeoutsRef.current.set(payload.DisplayName, timeoutId);
          
          // Call consumer callback
          onUserTyping?.(typingEvent);
        });

        // Store connection and attempt to connect
        connectionRef.current = connection;
        
        await connection.start();
        log(`Connected to SignalR hub with connection ID: ${connection.connectionId}`);
        setConnectionState('connected');
        onConnectionStateChange?.('connected');
        setError(null);
        
      } catch (err) {
        const errorMsg = err instanceof Error ? err.message : String(err);
        log('Failed to initialize SignalR connection', err);
        setError(errorMsg);
        setConnectionState('disconnected');
        onConnectionStateChange?.('disconnected');
      }
    };

    initializeConnection();

    // Cleanup on unmount
    return () => {
      // Clear all typing timeouts
      typingTimeoutsRef.current.forEach((timeout) => clearTimeout(timeout));
      typingTimeoutsRef.current.clear();
      
      if (connectionRef.current) {
        connectionRef.current.stop()
          .catch((err) => log('Error stopping connection', err));
      }
    };
  }, [log, onHandoff, onConnectionStateChange, onUserOnline, onUserOffline, onUserTyping, typingTimeoutMs]);

  // Get current agent from most recent handoff
  const getCurrentAgent = useCallback((): { agentId: string; agentName: string } | null => {
    if (handoffHistory.length === 0) {
      return null;
    }
    
    const lastHandoff = handoffHistory[handoffHistory.length - 1];
    return {
      agentId: lastHandoff.ToAgentId,
      agentName: lastHandoff.ToAgentName,
    };
  }, [handoffHistory]);

  // Clear handoff history
  const clearHistory = useCallback(() => {
    setHandoffHistory([]);
  }, []);

  // Clear presence data
  const clearPresence = useCallback(() => {
    setOnlineUsers([]);
    setTypingUsers([]);
    typingTimeoutsRef.current.forEach((timeout) => clearTimeout(timeout));
    typingTimeoutsRef.current.clear();
  }, []);

  return {
    // State
    connectionState,
    error,
    handoffHistory,
    onlineUsers,
    typingUsers,
    
    // Computed
    currentAgent: getCurrentAgent(),
    
    // Methods
    clearHistory,
    clearPresence,
    
    // Connection control
    connection: connectionRef.current,
  };
}
