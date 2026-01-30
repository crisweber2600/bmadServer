import React, { Component } from 'react';
import type { ErrorInfo } from 'react';
import { Button, Typography, Space } from 'antd';
import { ReloadOutlined, WarningOutlined } from '@ant-design/icons';
import './ErrorBoundary.css';

const { Title, Text, Paragraph } = Typography;

export interface ErrorBoundaryProps {
  /** Child components to render */
  children: React.ReactNode;
  /** Custom fallback UI to display on error */
  fallback?: React.ReactNode;
  /** Callback when an error is caught */
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
  /** Callback when error state is reset */
  onReset?: () => void;
  /** Keys that trigger automatic reset when changed */
  resetKeys?: unknown[];
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
}

/**
 * React Error Boundary with retry capability
 * 
 * Catches JavaScript errors anywhere in child component tree,
 * logs them, and displays a fallback UI instead of crashing.
 * 
 * @example
 * ```tsx
 * <ErrorBoundary
 *   onError={(error) => logErrorToService(error)}
 *   onReset={() => refetchData()}
 *   resetKeys={[someKey]}
 * >
 *   <MyComponent />
 * </ErrorBoundary>
 * ```
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
    };
  }

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    // Call onError callback or log to console
    if (this.props.onError) {
      this.props.onError(error, errorInfo);
    } else {
      console.error('[ErrorBoundary] Caught error:', error, errorInfo);
    }
  }

  componentDidUpdate(prevProps: ErrorBoundaryProps): void {
    // Reset error state when resetKeys change
    if (this.state.hasError && this.props.resetKeys) {
      const prevKeys = prevProps.resetKeys || [];
      const currentKeys = this.props.resetKeys;
      
      const hasKeyChanged = currentKeys.some((key, index) => {
        return key !== prevKeys[index];
      });
      
      if (hasKeyChanged) {
        this.resetErrorBoundary();
      }
    }
  }

  resetErrorBoundary = (): void => {
    this.props.onReset?.();
    this.setState({
      hasError: false,
      error: null,
    });
  };

  render(): React.ReactNode {
    if (this.state.hasError) {
      // Use custom fallback if provided
      if (this.props.fallback) {
        return this.props.fallback;
      }

      // Default fallback UI
      return (
        <div 
          className="error-boundary"
          role="alert"
          aria-live="assertive"
          data-testid="error-boundary"
        >
          <Space direction="vertical" align="center" size="large" className="error-content">
            <WarningOutlined className="error-icon" aria-hidden="true" />
            
            <Title level={3} className="error-title">
              Something went wrong
            </Title>
            
            <Text type="secondary" className="error-subtitle">
              An unexpected error occurred while rendering this component.
            </Text>
            
            {this.state.error && (
              <Paragraph
                className="error-message"
                code
                copyable
                ellipsis={{ rows: 3, expandable: true }}
              >
                {this.state.error.message}
              </Paragraph>
            )}
            
            <Button
              type="primary"
              icon={<ReloadOutlined />}
              onClick={this.resetErrorBoundary}
              size="large"
              data-testid="error-boundary-retry"
            >
              Try Again
            </Button>
          </Space>
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
