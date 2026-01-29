import { useState, useCallback, useRef, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';

/**
 * Online user information from USER_ONLINE/USER_OFFLINE events
 */
export interface OnlineUser {
  UserId: string;
  DisplayName: string;
  IsOnline: boolean;
  LastSeen: Date;
}

/**
 * Hook options for presence tracking
 */
export interface UsePresenceOptions {
  /** Workflow ID to scope presence tracking */
  workflowId: string;
  /** Base API URL for SignalR hub - defaults to environment variable */
  apiUrl?: string;
  /** Callback when a user comes online */
  onUserOnline?: (user: OnlineUser) => void;
  /** Callback when a user goes offline */
  onUserOffline?: (user: OnlineUser) => void;
  /** Callback when a user is typing */
  onUserTyping?: (displayName: string) => void;
  /** Enable debug logging */
  debug?: boolean;
  /** Typing timeout in milliseconds - default 3000 */
  typingTimeoutMs?: number;
}

/**
 * Return type for usePresence hook
 */
export interface UsePresenceReturn {
  /** List of currently online users */
  onlineUsers: OnlineUser[];
  /** List of currently typing user names */
  typingUsers: string[];
  /** Connection state */
  connectionState: 'connected' | 'reconnecting' | 'disconnected';
  /** Any error message */
  error: string | null;
}

/**
 * Hook for tracking user presence via SignalR events
 * 
 * Listens for USER_ONLINE, USER_OFFLINE, and USER_TYPING events.
 * Manages typing indicator state with automatic timeout.
 * 
 * @example
 * ```tsx
 * const { onlineUsers, typingUsers, connectionState } = usePresence({
 *   workflowId: 'workflow-123',
 *   onUserOnline: (user) => console.log(`${user.DisplayName} joined`),
 * });
 * ```
 */
export function usePresence(options: UsePresenceOptions): UsePresenceReturn {
  const {
    workflowId,
    apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:8080',
    onUserOnline,
    onUserOffline,
    onUserTyping,
    debug = false,
    typingTimeoutMs = 3000,
  } = options;

  const [onlineUsers, setOnlineUsers] = useState<OnlineUser[]>([]);
  const [typingUsers, setTypingUsers] = useState<string[]>([]);
  const [connectionState, setConnectionState] = useState<'connected' | 'reconnecting' | 'disconnected'>('disconnected');
  const [error, setError] = useState<string | null>(null);
  
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const typingTimeoutsRef = useRef<Map<string, NodeJS.Timeout>>(new Map());

  // Debug logging helper
  const log = useCallback((message: string, data?: unknown) => {
    if (debug) {
      console.log(`[usePresence] ${message}`, data || '');
    }
  }, [debug]);

  // Parse event payload with validation
  const parseUserEvent = useCallback((payload: unknown, eventName: string): {
    UserId: string;
    DisplayName: string;
    IsOnline?: boolean;
    LastSeen?: string;
  } | null => {
    if (!payload || typeof payload !== 'object') {
      console.warn(`[usePresence] ${eventName}: Malformed payload - not an object`, payload);
      return null;
    }
    
    const p = payload as Record<string, unknown>;
    
    if (typeof p.UserId !== 'string' || !p.UserId) {
      console.warn(`[usePresence] ${eventName}: Malformed payload - missing UserId`, payload);
      return null;
    }
    
    if (typeof p.DisplayName !== 'string') {
      console.warn(`[usePresence] ${eventName}: Malformed payload - missing DisplayName`, payload);
      return null;
    }
    
    return {
      UserId: p.UserId,
      DisplayName: p.DisplayName,
      IsOnline: typeof p.IsOnline === 'boolean' ? p.IsOnline : undefined,
      LastSeen: typeof p.LastSeen === 'string' ? p.LastSeen : undefined,
    };
  }, []);

  // Clear typing indicator with timeout
  const startTypingTimeout = useCallback((displayName: string) => {
    // Clear existing timeout for this user
    const existingTimeout = typingTimeoutsRef.current.get(displayName);
    if (existingTimeout) {
      clearTimeout(existingTimeout);
    }
    
    // Set new timeout
    const timeout = setTimeout(() => {
      setTypingUsers((prev) => prev.filter((name) => name !== displayName));
      typingTimeoutsRef.current.delete(displayName);
    }, typingTimeoutMs);
    
    typingTimeoutsRef.current.set(displayName, timeout);
  }, [typingTimeoutMs]);

  // Initialize SignalR connection
  useEffect(() => {
    const initializeConnection = async () => {
      try {
        log('Initializing presence SignalR connection...');
        
        const hubUrl = `${apiUrl}/hubs/chat`;
        
        const connection = new signalR.HubConnectionBuilder()
          .withUrl(hubUrl, {
            accessTokenFactory: async () => {
              return localStorage.getItem('accessToken') || '';
            },
          })
          .withAutomaticReconnect([0, 2000, 10000, 30000])
          .configureLogging(debug ? signalR.LogLevel.Information : signalR.LogLevel.Error)
          .build();

        // Connection state handlers
        connection.onreconnecting(() => {
          log('Connection lost. Reconnecting...');
          setConnectionState('reconnecting');
        });

        connection.onreconnected(() => {
          log('Reconnected successfully');
          setConnectionState('connected');
          setError(null);
          // Clear and refetch users on reconnect
          setOnlineUsers([]);
          setTypingUsers([]);
        });

        connection.onclose((err) => {
          log('Connection closed', err);
          setConnectionState('disconnected');
          if (err) {
            setError('Connection lost. Please refresh the page.');
          }
        });

        // USER_ONLINE handler
        connection.on('USER_ONLINE', (payload: unknown) => {
          const parsed = parseUserEvent(payload, 'USER_ONLINE');
          if (!parsed) return;
          
          log('USER_ONLINE received', parsed);
          
          const newUser: OnlineUser = {
            UserId: parsed.UserId,
            DisplayName: parsed.DisplayName,
            IsOnline: true,
            LastSeen: parsed.LastSeen ? new Date(parsed.LastSeen) : new Date(),
          };
          
          setOnlineUsers((prev) => {
            // Remove if exists, then add
            const filtered = prev.filter((u) => u.UserId !== newUser.UserId);
            return [...filtered, newUser];
          });
          
          onUserOnline?.(newUser);
        });

        // USER_OFFLINE handler
        connection.on('USER_OFFLINE', (payload: unknown) => {
          const parsed = parseUserEvent(payload, 'USER_OFFLINE');
          if (!parsed) return;
          
          log('USER_OFFLINE received', parsed);
          
          const offlineUser: OnlineUser = {
            UserId: parsed.UserId,
            DisplayName: parsed.DisplayName,
            IsOnline: false,
            LastSeen: parsed.LastSeen ? new Date(parsed.LastSeen) : new Date(),
          };
          
          setOnlineUsers((prev) => prev.filter((u) => u.UserId !== parsed.UserId));
          
          // Also remove from typing users
          setTypingUsers((prev) => prev.filter((name) => name !== parsed.DisplayName));
          
          // Clear typing timeout
          const timeout = typingTimeoutsRef.current.get(parsed.DisplayName);
          if (timeout) {
            clearTimeout(timeout);
            typingTimeoutsRef.current.delete(parsed.DisplayName);
          }
          
          onUserOffline?.(offlineUser);
        });

        // USER_TYPING handler
        connection.on('USER_TYPING', (payload: unknown) => {
          const parsed = parseUserEvent(payload, 'USER_TYPING');
          if (!parsed) return;
          
          log('USER_TYPING received', parsed);
          
          setTypingUsers((prev) => {
            if (prev.includes(parsed.DisplayName)) {
              return prev; // Already in list
            }
            return [...prev, parsed.DisplayName];
          });
          
          startTypingTimeout(parsed.DisplayName);
          onUserTyping?.(parsed.DisplayName);
        });

        connectionRef.current = connection;
        
        await connection.start();
        log(`Connected with ID: ${connection.connectionId}`);
        setConnectionState('connected');
        setError(null);
        
        // Join workflow group to receive events
        if (workflowId) {
          try {
            await connection.invoke('JoinWorkflow', workflowId);
            log(`Joined workflow: ${workflowId}`);
          } catch (err) {
            log('Failed to join workflow (may not be implemented)', err);
          }
        }
        
      } catch (err) {
        const errorMsg = err instanceof Error ? err.message : String(err);
        log('Failed to initialize connection', err);
        setError(errorMsg);
        setConnectionState('disconnected');
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
  }, [
    apiUrl,
    workflowId,
    debug,
    log,
    parseUserEvent,
    startTypingTimeout,
    onUserOnline,
    onUserOffline,
    onUserTyping,
  ]);

  return {
    onlineUsers,
    typingUsers,
    connectionState,
    error,
  };
}
