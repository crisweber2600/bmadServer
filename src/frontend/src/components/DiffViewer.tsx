import React from 'react';
import { Typography, Empty, Row, Col, Card, Tag } from 'antd';
import { MinusCircleOutlined, PlusCircleOutlined, SwapOutlined } from '@ant-design/icons';
import './DiffViewer.css';

const { Text, Title, Paragraph } = Typography;

/**
 * Represents a single field change between versions
 */
export interface FieldChange {
  Field: string;
  OldValue: string;
  NewValue: string;
}

/**
 * Props for version-based diff view (array of field changes)
 */
export interface VersionDiffProps {
  /** Array of field changes to display */
  changes: FieldChange[];
  /** Source version number */
  fromVersion: number;
  /** Target version number */
  toVersion: number;
  /** Optional title for the diff view */
  title?: string;
}

/**
 * Props for content-based diff view (side-by-side content comparison)
 */
export interface ContentDiffProps {
  /** Original content string */
  originalContent: string;
  /** Modified content string */
  modifiedContent: string;
  /** Label for original content */
  originalLabel?: string;
  /** Label for modified content */
  modifiedLabel?: string;
}

export type DiffViewerProps = VersionDiffProps | ContentDiffProps;

// Type guards
function isVersionDiff(props: DiffViewerProps): props is VersionDiffProps {
  return 'changes' in props && 'fromVersion' in props;
}

function isContentDiff(props: DiffViewerProps): props is ContentDiffProps {
  return 'originalContent' in props && 'modifiedContent' in props;
}

/**
 * DiffViewer - Side-by-side diff display for decision versions
 * 
 * Supports two modes:
 * 1. Version diff: Shows field-by-field changes between versions
 * 2. Content diff: Shows side-by-side content comparison
 */
export const DiffViewer: React.FC<DiffViewerProps> = (props) => {
  // Content comparison mode
  if (isContentDiff(props)) {
    const { originalContent, modifiedContent, originalLabel, modifiedLabel } = props;
    
    return (
      <div className="diff-viewer content-mode" data-testid="diff-viewer">
        <Row gutter={16} className="diff-content-comparison">
          <Col span={12}>
            <Card 
              size="small" 
              className="content-card original"
              title={originalLabel || 'Original'}
            >
              <Paragraph className="content-text" data-testid="original-content">
                {originalContent || <Text type="secondary"><em>(empty)</em></Text>}
              </Paragraph>
            </Card>
          </Col>
          <Col span={12}>
            <Card 
              size="small" 
              className="content-card modified"
              title={modifiedLabel || 'Modified'}
            >
              <Paragraph className="content-text" data-testid="modified-content">
                {modifiedContent || <Text type="secondary"><em>(empty)</em></Text>}
              </Paragraph>
            </Card>
          </Col>
        </Row>
      </div>
    );
  }

  // Version diff mode (default)
  if (isVersionDiff(props)) {
    const { changes, fromVersion, toVersion, title } = props;
    
    if (!changes || changes.length === 0) {
      return (
        <div className="diff-viewer-empty" data-testid="diff-viewer-empty">
          <Empty description="No differences found" />
        </div>
      );
    }

    return (
      <div className="diff-viewer version-mode" data-testid="diff-viewer">
        {title && (
          <Title level={5} className="diff-title">
            <SwapOutlined /> {title}
          </Title>
        )}
        
        <Row gutter={16} className="diff-header">
          <Col span={12}>
            <Card size="small" className="version-header-card from-version">
              <Text strong>Version {fromVersion}</Text>
              <Tag color="red" className="version-tag">Previous</Tag>
            </Card>
          </Col>
          <Col span={12}>
            <Card size="small" className="version-header-card to-version">
              <Text strong>Version {toVersion}</Text>
              <Tag color="green" className="version-tag">Current</Tag>
            </Card>
          </Col>
        </Row>

        <div className="diff-changes" data-testid="diff-changes">
          {changes.map((change, index) => (
            <div 
              key={`${change.Field}-${index}`} 
              className="diff-row"
              data-testid={`diff-row-${index}`}
            >
              <div className="diff-field-name">
                <Text strong>{change.Field}</Text>
              </div>
              
              <Row gutter={16} className="diff-values">
                <Col span={12}>
                  <div className="diff-value old-value" data-testid={`old-value-${index}`}>
                    <MinusCircleOutlined className="diff-icon removed" />
                    <Text 
                      delete={!!change.OldValue} 
                      type={change.OldValue ? 'danger' : 'secondary'}
                    >
                      {change.OldValue || <em>(empty)</em>}
                    </Text>
                  </div>
                </Col>
                <Col span={12}>
                  <div className="diff-value new-value" data-testid={`new-value-${index}`}>
                    <PlusCircleOutlined className="diff-icon added" />
                    <Text 
                      type={change.NewValue ? 'success' : 'secondary'}
                    >
                      {change.NewValue || <em>(empty)</em>}
                    </Text>
                  </div>
                </Col>
              </Row>
            </div>
          ))}
        </div>

        <div className="diff-summary">
          <Text type="secondary">
            {changes.length} field{changes.length !== 1 ? 's' : ''} changed
          </Text>
        </div>
      </div>
    );
  }

  // Fallback for invalid props
  return (
    <div className="diff-viewer-empty" data-testid="diff-viewer-empty">
      <Empty description="Invalid diff configuration" />
    </div>
  );
};
