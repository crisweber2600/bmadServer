import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Space, Alert, Spin, Typography, notification } from 'antd';
import { LoginOutlined, LogoutOutlined, SendOutlined } from '@ant-design/icons';
import * as signalR from '@microsoft/signalr';
import {
    ChatContainer,
    ChatMessage,
    TypingIndicator,
    WorkflowSelector,
    ConversationActions,
    WorkflowStatusBar,
} from './components';
import { useWorkflows } from './hooks';
import './ChatDemo.css';

const { Text, Title } = Typography;

interface Message {
    id: string;
    content: string;
    isUser: boolean;
    timestamp: Date;
    agentName?: string;
    role?: string;
}

interface AuthState {
    isAuthenticated: boolean;
    token: string | null;
    user: { email: string; displayName: string } | null;
}

interface SessionRestoredData {
    Id: string;
    WorkflowName?: string;
    CurrentStep?: string;
    ConversationHistory?: Array<{
        Id: string;
        Role: string;
        Content: string;
        Timestamp: string;
    }>;
    Message: string;
}

export function ChatApp() {
    const [messages, setMessages] = useState<Message[]>([]);
    const [inputValue, setInputValue] = useState('');
    const [isTyping, setIsTyping] = useState(false);
    const [authState, setAuthState] = useState<AuthState>({
        isAuthenticated: false,
        token: null,
        user: null,
    });
    const [loginForm, setLoginForm] = useState({ email: '', password: '', displayName: '' });
    const [isRegistering, setIsRegistering] = useState(false);
    const [connectionState, setConnectionState] = useState<'disconnected' | 'connecting' | 'connected'>('disconnected');
    const [error, setError] = useState<string | null>(null);
    const connectionRef = useRef<signalR.HubConnection | null>(null);

    // Workflow state
    const [selectedWorkflowId, setSelectedWorkflowId] = useState<string | undefined>();

    // Get API base URL from environment or use proxy
    const apiUrl = '';

    // Workflow management hook
    const workflows = useWorkflows({ token: authState.token, apiUrl });

    const isTerminalWorkflowStatus = (status?: string | null) => {
        if (!status) return false;
        return ['cancelled', 'completed', 'failed'].includes(status.toLowerCase());
    };

    const hasActiveWorkflow =
        workflows.currentWorkflow !== null &&
        !isTerminalWorkflowStatus(workflows.currentWorkflow.status);

    // Fetch workflow definitions when authenticated
    useEffect(() => {
        if (authState.isAuthenticated) {
            workflows.fetchDefinitions();
        }
    }, [authState.isAuthenticated]);

    // Registration handler
    const handleRegister = async () => {
        setError(null);
        try {
            const response = await fetch(`${apiUrl}/api/v1/auth/register`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    email: loginForm.email,
                    password: loginForm.password,
                    displayName: loginForm.displayName || loginForm.email.split('@')[0],
                }),
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.detail || 'Registration failed');
            }

            notification.success({
                message: 'Registration Successful',
                description: 'You can now sign in with your credentials.',
            });
            setIsRegistering(false);
        } catch (err) {
            const errorMessage = err instanceof Error ? err.message : 'Registration failed';
            setError(errorMessage);
            notification.error({
                message: 'Registration Failed',
                description: errorMessage,
            });
        }
    };

    // Login handler
    const handleLogin = async () => {
        setError(null);
        try {
            const response = await fetch(`${apiUrl}/api/v1/auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    email: loginForm.email,
                    password: loginForm.password,
                }),
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.detail || 'Login failed');
            }

            const data = await response.json();
            const token = data.accessToken;

            localStorage.setItem('jwt_token', token);
            setAuthState({
                isAuthenticated: true,
                token,
                user: { email: data.email, displayName: data.displayName },
            });

            notification.success({
                message: 'Login Successful',
                description: `Welcome, ${data.displayName}!`,
            });
        } catch (err) {
            const errorMessage = err instanceof Error ? err.message : 'Login failed';
            setError(errorMessage);
            notification.error({
                message: 'Login Failed',
                description: errorMessage,
            });
        }
    };

    // Logout handler
    const handleLogout = useCallback(() => {
        localStorage.removeItem('jwt_token');
        connectionRef.current?.stop();
        setAuthState({ isAuthenticated: false, token: null, user: null });
        setMessages([]);
        setConnectionState('disconnected');
    }, []);

    // Connect to SignalR hub
    const connectToHub = useCallback(async (token: string) => {
        if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
            return;
        }

        setConnectionState('connecting');

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${apiUrl}/hubs/chat`, {
                accessTokenFactory: () => token,
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Handle incoming messages (handle both PascalCase and camelCase from server)
        connection.on('ReceiveMessage', (message: Record<string, unknown>) => {
            setIsTyping(false);
            // Handle both casing conventions from server
            const role = (message.Role || message.role) as string;
            const content = (message.Content || message.content) as string;
            const timestampStr = (message.Timestamp || message.timestamp) as string | undefined;
            const agentName = (message.AgentName || message.agentName) as string | undefined;

            const timestamp = timestampStr ? new Date(timestampStr) : new Date();
            // Validate timestamp - fallback to current time if invalid
            const validTimestamp = isNaN(timestamp.getTime()) ? new Date() : timestamp;
            setMessages((prev) => [
                ...prev,
                {
                    id: `msg-${Date.now()}`,
                    content: content || '',
                    isUser: role === 'user',
                    timestamp: validTimestamp,
                    agentName: agentName || (role === 'assistant' || role === 'system' ? 'BMAD Agent' : undefined),
                    role: role,
                },
            ]);
        });

        // Handle session restored
        connection.on('SESSION_RESTORED', (data: SessionRestoredData) => {
            notification.info({
                message: 'Session Restored',
                description: data.Message,
            });

            if (data.ConversationHistory) {
                const restoredMessages: Message[] = data.ConversationHistory.map((msg) => ({
                    id: msg.Id,
                    content: msg.Content,
                    isUser: msg.Role === 'user',
                    timestamp: new Date(msg.Timestamp),
                    agentName: msg.Role === 'assistant' ? 'BMAD Agent' : undefined,
                    role: msg.Role,
                }));
                setMessages(restoredMessages);
            }
        });

        // Handle typing indicator
        connection.on('AgentTyping', (data: { IsTyping: boolean }) => {
            setIsTyping(data.IsTyping);
        });

        // Handle workflow status
        connection.on('WORKFLOW_STATUS_CHANGED', (data: { status: string }) => {
            notification.info({
                message: 'Workflow Status',
                description: `Workflow status: ${data.status}`,
            });
        });

        // Handle connection state changes
        connection.onreconnecting(() => {
            setConnectionState('connecting');
            notification.warning({
                message: 'Reconnecting',
                description: 'Connection lost. Attempting to reconnect...',
            });
        });

        connection.onreconnected(() => {
            setConnectionState('connected');
            notification.success({
                message: 'Reconnected',
                description: 'Connection restored.',
            });
        });

        connection.onclose(() => {
            setConnectionState('disconnected');
        });

        try {
            await connection.start();
            connectionRef.current = connection;
            setConnectionState('connected');
            notification.success({
                message: 'Connected',
                description: 'Connected to chat server.',
            });
        } catch (err) {
            console.error('SignalR connection error:', err);
            setConnectionState('disconnected');
            setError('Failed to connect to chat server');
        }
    }, [apiUrl]);

    // Auto-connect when authenticated
    useEffect(() => {
        if (authState.isAuthenticated && authState.token) {
            connectToHub(authState.token);
        }

        return () => {
            connectionRef.current?.stop();
        };
    }, [authState.isAuthenticated, authState.token, connectToHub]);

    // Check for existing token on mount
    useEffect(() => {
        const token = localStorage.getItem('jwt_token');
        if (token) {
            // Validate token by fetching user info
            fetch(`${apiUrl}/api/v1/auth/me`, {
                headers: { Authorization: `Bearer ${token}` },
            })
                .then((res) => {
                    if (res.ok) return res.json();
                    throw new Error('Invalid token');
                })
                .then((user) => {
                    setAuthState({
                        isAuthenticated: true,
                        token,
                        user: { email: user.email, displayName: user.displayName },
                    });
                })
                .catch(() => {
                    localStorage.removeItem('jwt_token');
                });
        }
    }, [apiUrl]);

    // Debug logging
    useEffect(() => {
        console.log('[DEBUG] ChatApp State:', {
            connectionState,
            selectedWorkflowId,
            hasActiveWorkflow: workflows.currentWorkflow !== null,
            isAuthenticated: authState.isAuthenticated,
            isLoading: workflows.isLoading
        });
    }, [connectionState, selectedWorkflowId, workflows.currentWorkflow, authState.isAuthenticated, workflows.isLoading]);

    // Send message handler
    const handleSend = async (messageOverride?: string) => {
        const messageToSend = messageOverride || inputValue;
        if (!messageToSend.trim() || !connectionRef.current) return;

        const userMessage: Message = {
            id: `user-${Date.now()}`,
            content: messageToSend,
            isUser: true,
            timestamp: new Date(),
        };

        setMessages((prev) => [...prev, userMessage]);
        if (!messageOverride) {
            setInputValue('');
        }
        setIsTyping(true);

        try {
            await connectionRef.current.invoke('SendMessage', messageToSend);
        } catch (err) {
            console.error('Failed to send message:', err);
            setIsTyping(false);
            notification.error({
                message: 'Send Failed',
                description: 'Failed to send message. Please try again.',
            });
        }
    };

    // Workflow handlers
    const handleStartWorkflow = async (workflowId: string) => {
        const instance = await workflows.createWorkflow({ workflowId });
        if (instance) {
            const started = await workflows.startWorkflow(instance.id);
            if (started) {
                notification.success({
                    message: 'Workflow Started',
                    description: `Started ${workflowId} workflow`,
                });
                // Send initial message to engage the workflow
                await handleSend(`Start ${workflowId} workflow`);
            }
        }
    };

    const handleConversationAction = (message: string) => {
        handleSend(message);
    };

    // Render login form if not authenticated
    if (!authState.isAuthenticated) {
        return (
            <div className="chat-demo">
                <div className="chat-header">
                    <Title level={2}>BMAD Chat</Title>
                    <Text type="secondary">{isRegistering ? 'Create an account' : 'Sign in to start chatting'}</Text>
                </div>

                <div className="login-form-container">
                    {error && (
                        <Alert
                            message="Error"
                            description={error}
                            type="error"
                            showIcon
                            closable
                            onClose={() => setError(null)}
                            style={{ marginBottom: 16 }}
                        />
                    )}

                    <Space direction="vertical" style={{ width: '100%' }} size="middle">
                        <Input
                            placeholder="Email"
                            value={loginForm.email}
                            onChange={(e) => setLoginForm({ ...loginForm, email: e.target.value })}
                            onPressEnter={isRegistering ? handleRegister : handleLogin}
                        />
                        {isRegistering && (
                            <Input
                                placeholder="Display Name"
                                value={loginForm.displayName}
                                onChange={(e) => setLoginForm({ ...loginForm, displayName: e.target.value })}
                                onPressEnter={handleRegister}
                            />
                        )}
                        <Input.Password
                            placeholder="Password"
                            value={loginForm.password}
                            onChange={(e) => setLoginForm({ ...loginForm, password: e.target.value })}
                            onPressEnter={isRegistering ? handleRegister : handleLogin}
                        />
                        <Button
                            type="primary"
                            icon={<LoginOutlined />}
                            onClick={isRegistering ? handleRegister : handleLogin}
                            block
                        >
                            {isRegistering ? 'Register' : 'Sign In'}
                        </Button>
                    </Space>

                    <div className="login-footer">
                        <Button type="link" onClick={() => setIsRegistering(!isRegistering)}>
                            {isRegistering ? 'Already have an account? Sign In' : "Don't have an account? Register"}
                        </Button>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="chat-demo">
            <div className="chat-header">
                <div className="chat-header-content">
                    <div>
                        <Title level={2} style={{ margin: 0 }}>BMAD Chat</Title>
                        <Text type="secondary">
                            {connectionState === 'connected' ? (
                                <span className="status-connected">● Connected</span>
                            ) : connectionState === 'connecting' ? (
                                <span className="status-connecting">● Connecting...</span>
                            ) : (
                                <span className="status-disconnected">● Disconnected</span>
                            )}
                            {' '} | {authState.user?.displayName}
                        </Text>
                    </div>
                    <Button
                        icon={<LogoutOutlined />}
                        onClick={handleLogout}
                        danger
                    >
                        Sign Out
                    </Button>
                </div>
            </div>

            {/* Workflow Selection Bar */}
            <div className="workflow-bar">
                <WorkflowSelector
                    definitions={workflows.definitions}
                    selectedWorkflowId={selectedWorkflowId}
                    hasActiveWorkflow={hasActiveWorkflow}
                    isLoading={workflows.isLoading}
                    onSelect={setSelectedWorkflowId}
                    onStart={handleStartWorkflow}
                    disabled={connectionState !== 'connected'}
                />

                {/* Active Workflow Status */}
                {hasActiveWorkflow && workflows.currentWorkflow && (
                    <WorkflowStatusBar
                        status={workflows.currentWorkflow}
                        isLoading={workflows.isLoading}
                        onPause={() => workflows.currentWorkflow && workflows.pauseWorkflow(workflows.currentWorkflow.id)}
                        onResume={() => workflows.currentWorkflow && workflows.resumeWorkflow(workflows.currentWorkflow.id)}
                        onCancel={() => workflows.currentWorkflow && workflows.cancelWorkflow(workflows.currentWorkflow.id)}
                        disabled={connectionState !== 'connected'}
                    />
                )}
            </div>

            <div className="chat-messages">
                {connectionState === 'connecting' ? (
                    <div className="centered-content">
                        <Spin size="large" />
                        <div className="centered-content-spacing">
                            <Text type="secondary">Connecting to chat server...</Text>
                        </div>
                    </div>
                ) : (
                    <ChatContainer>
                        {messages.length === 0 ? (
                            <div className="centered-content">
                                <Text type="secondary">
                                    Select a workflow above and start chatting!
                                </Text>
                            </div>
                        ) : (
                            messages.map((msg) => (
                                <ChatMessage
                                    key={msg.id}
                                    content={msg.content}
                                    isUser={msg.isUser}
                                    timestamp={msg.timestamp}
                                    agentName={msg.agentName}
                                />
                            ))
                        )}
                        {isTyping && <TypingIndicator agentName="BMAD Agent" />}
                    </ChatContainer>
                )}
            </div>

            {/* Conversation Quick Actions */}
            <div className="conversation-actions-bar">
                <ConversationActions
                    onAction={handleConversationAction}
                    isProcessing={isTyping}
                    hasActiveWorkflow={workflows.currentWorkflow !== null}
                    disabled={connectionState !== 'connected'}
                />
            </div>

            <div className="chat-input">
                <Space.Compact style={{ width: '100%' }}>
                    <Input
                        placeholder="Type your message..."
                        value={inputValue}
                        onChange={(e) => setInputValue(e.target.value)}
                        onPressEnter={() => handleSend()}
                        disabled={isTyping || connectionState !== 'connected'}
                    />
                    <Button
                        type="primary"
                        icon={<SendOutlined />}
                        onClick={() => handleSend()}
                        disabled={isTyping || connectionState !== 'connected'}
                    >
                        Send
                    </Button>
                </Space.Compact>
            </div>
        </div>
    );
}
