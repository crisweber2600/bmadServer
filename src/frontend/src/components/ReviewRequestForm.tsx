import React, { useState, useEffect, useCallback } from 'react';
import { Modal, Form, Select, DatePicker, Button, Alert, Spin, Space, Typography } from 'antd';
import { UserOutlined, CalendarOutlined, SendOutlined, ReloadOutlined } from '@ant-design/icons';
import type { Dayjs } from 'dayjs';
import './ReviewRequestForm.css';

const { Text } = Typography;

export interface Reviewer {
  Id: string;
  DisplayName: string;
  Email: string;
}

export interface ReviewRequestFormProps {
  /** The decision ID for which to request review */
  decisionId: string;
  /** The workflow ID for context */
  workflowId: string;
  /** Whether the form modal is open */
  open: boolean;
  /** Callback when form is closed */
  onClose: () => void;
  /** Callback on successful review request */
  onSuccess?: () => void;
  /** Pre-fetched list of available reviewers */
  availableReviewers?: Reviewer[];
  /** Async function to fetch reviewers (lazy loading) */
  fetchReviewers?: () => Promise<Reviewer[]>;
  /** Callback to submit review request */
  onSubmit?: (data: ReviewRequestData) => Promise<void>;
}

export interface ReviewRequestData {
  decisionId: string;
  reviewerIds: string[];
  deadline?: string; // ISO date string
  notes?: string;
}

interface FormValues {
  reviewerIds: string[];
  deadline?: Dayjs | null;
}

/**
 * ReviewRequestForm - Form for requesting decision review
 * 
 * Allows users to select reviewers and optionally set a deadline
 * for reviewing a decision. Supports both pre-fetched reviewers
 * and lazy-loading via async function.
 */
export const ReviewRequestForm: React.FC<ReviewRequestFormProps> = ({
  decisionId,
  workflowId,
  open,
  onClose,
  onSuccess,
  availableReviewers,
  fetchReviewers,
  onSubmit,
}) => {
  const [form] = Form.useForm<FormValues>();
  const [reviewers, setReviewers] = useState<Reviewer[]>(availableReviewers || []);
  const [isLoadingReviewers, setIsLoadingReviewers] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Fetch reviewers when modal opens (if using lazy loading)
  const loadReviewers = useCallback(async () => {
    if (!fetchReviewers) return;

    setIsLoadingReviewers(true);
    setLoadError(null);

    try {
      const fetchedReviewers = await fetchReviewers();
      setReviewers(fetchedReviewers);
    } catch (error) {
      setLoadError('Failed to load reviewers');
      console.error('Error fetching reviewers:', error);
    } finally {
      setIsLoadingReviewers(false);
    }
  }, [fetchReviewers]);

  useEffect(() => {
    if (open) {
      // If we have pre-fetched reviewers, use them
      if (availableReviewers) {
        setReviewers(availableReviewers);
      } else if (fetchReviewers && reviewers.length === 0) {
        // Otherwise, try to fetch them
        loadReviewers();
      }
    }
  }, [open, availableReviewers, fetchReviewers, loadReviewers, reviewers.length]);

  const handleSubmit = async (values: FormValues) => {
    if (!onSubmit) return;

    setIsSubmitting(true);

    try {
      const data: ReviewRequestData = {
        decisionId,
        reviewerIds: values.reviewerIds,
        deadline: values.deadline?.toISOString(),
      };

      await onSubmit(data);
      form.resetFields();
      onSuccess?.();
      onClose();
    } catch (error) {
      console.error('Error submitting review request:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    form.resetFields();
    onClose();
  };

  const handleRetry = () => {
    loadReviewers();
  };

  return (
    <Modal
      title={
        <Space>
          <UserOutlined />
          <span>Request Review</span>
        </Space>
      }
      open={open}
      onCancel={handleClose}
      footer={null}
      className="review-request-modal"
      data-testid="review-request-form"
    >
      {isLoadingReviewers ? (
        <div className="loading-container" data-testid="loading-reviewers">
          <Spin size="large" />
          <Text type="secondary">Loading reviewers...</Text>
        </div>
      ) : loadError ? (
        <div className="error-container" data-testid="load-error">
          <Alert
            type="error"
            message={loadError}
            action={
              <Button 
                size="small" 
                icon={<ReloadOutlined />} 
                onClick={handleRetry}
                data-testid="retry-button"
              >
                Retry
              </Button>
            }
          />
        </div>
      ) : (
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          data-testid="review-form"
        >
          <Form.Item
            name="reviewerIds"
            label="Select Reviewers"
            rules={[
              { required: true, message: 'Select at least one reviewer' },
              { type: 'array', min: 1, message: 'Select at least one reviewer' },
            ]}
          >
            <Select
              mode="multiple"
              placeholder="Choose reviewers"
              optionFilterProp="label"
              showSearch
              data-testid="reviewer-select"
              options={reviewers.map((reviewer) => ({
                value: reviewer.Id,
                label: reviewer.DisplayName,
                title: reviewer.Email,
              }))}
              optionRender={(option) => (
                <div className="reviewer-option">
                  <span className="reviewer-name">{option.label}</span>
                  <span className="reviewer-email">{option.data.title}</span>
                </div>
              )}
            />
          </Form.Item>

          <Form.Item
            name="deadline"
            label={
              <Space>
                <CalendarOutlined />
                <span>Review Deadline (optional)</span>
              </Space>
            }
          >
            <DatePicker 
              showTime 
              format="YYYY-MM-DD HH:mm"
              placeholder="Select deadline"
              style={{ width: '100%' }}
              data-testid="deadline-picker"
            />
          </Form.Item>

          <Form.Item className="form-actions">
            <Space style={{ width: '100%', justifyContent: 'flex-end' }}>
              <Button onClick={handleClose} data-testid="cancel-button">
                Cancel
              </Button>
              <Button
                type="primary"
                htmlType="submit"
                icon={<SendOutlined />}
                loading={isSubmitting}
                data-testid="submit-button"
              >
                Request Review
              </Button>
            </Space>
          </Form.Item>
        </Form>
      )}
    </Modal>
  );
};
