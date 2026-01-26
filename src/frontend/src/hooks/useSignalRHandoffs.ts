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
 * Hook options for SignalR handoff handling
 */
export interface UseSignalRHandoffsOptions {
  /** Callback when a handoff event is received */
  onHandoff?: (event: AgentHandoffEvent) => void;
  /** Callback when connection state changes */
  onConnectionStateChange?: (state: 'connected' | 'reconnecting' | 'disconnected') => void;
  /** Enable debug logging */
  debug?: boolean;
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
  const { onHandoff, onConnectionStateChange, debug = false } = options || {};
  
  // Connection state
  const [connectionState, setConnectionState] = useState<'connected' | 'reconnecting' | 'disconnected'>('disconnected');
  const [error, setError] = useState<string | null>(null);
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const reconnectAttemptsRef = useRef(0);
  const maxReconnectAttemptsRef = useRef(10);
  
  // Handoff history
  const [handoffHistory, setHandoffHistory] = useState<AgentHandoffEvent[]>([]);

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
      if (connectionRef.current) {
        connectionRef.current.stop()
          .catch((err) => log('Error stopping connection', err));
      }
    };
  }, [log, onHandoff, onConnectionStateChange]);

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

  return {
    // State
    connectionState,
    error,
    handoffHistory,
    
    // Computed
    currentAgent: getCurrentAgent(),
    
    // Methods
    clearHistory,
    
    // Connection control
    connection: connectionRef.current,
  };
}
