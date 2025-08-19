import React from "react";
import { useDispatch, useSelector } from "react-redux";
import styled from "styled-components";
import { theme } from "../../styles/theme";
import { RootState, AppDispatch } from "../../store";
import { clearNotifications } from "../../store/slices/streamingSlice";
import { Card, CardHeader, CardTitle, CardContent } from "../common/Card";
import { Button } from "../common/Button";

const NotificationContainer = styled(Card)`
  height: fit-content;
  max-height: 600px;
  display: flex;
  flex-direction: column;
`;

const NotificationList = styled.div`
  max-height: 500px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: ${theme.sizes.spacing.md};
`;

const NotificationItem = styled.div<{ type: string }>`
  background-color: ${theme.colors.surfaceAlt};
  padding: ${theme.sizes.spacing.md};
  border-radius: ${theme.sizes.borderRadius};
  border-left: 4px solid
    ${(props) =>
      props.type === "PersonDetected"
        ? theme.colors.success
        : theme.colors.error};
`;

const NotificationHeader = styled.div`
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: ${theme.sizes.spacing.sm};
`;

const NotificationTitle = styled.div`
  font-weight: 600;
  color: ${theme.colors.text};
  font-size: 14px;
`;

const NotificationTime = styled.div`
  font-size: 11px;
  color: ${theme.colors.textSecondary};
`;

const NotificationMessage = styled.div`
  font-size: 13px;
  color: ${theme.colors.text};
  line-height: 1.4;
`;

const DetectionDetails = styled.div`
  margin-top: ${theme.sizes.spacing.sm};
  font-size: 12px;
  color: ${theme.colors.textSecondary};
`;

const EmptyState = styled.div`
  text-align: center;
  color: ${theme.colors.textSecondary};
  font-size: 14px;
  padding: ${theme.sizes.spacing.xl};
`;

export const NotificationPanel: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { notifications, detectionEvents } = useSelector(
    (state: RootState) => state.streaming,
  );

  const handleClearNotifications = () => {
    dispatch(clearNotifications());
  };

  const formatTime = (timestamp: string) => {
    return new Date(timestamp).toLocaleTimeString();
  };

  const allNotifications = [
    ...notifications.map((n) => ({ ...n, source: "notification" })),
    ...detectionEvents.map((e) => ({
      type: "PersonDetected",
      cameraId: e.cameraId,
      timestamp: e.timestamp,
      message: `Person detected on ${e.cameraName}`,
      detections: e.detections,
      source: "detection",
    })),
  ].sort(
    (a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime(),
  );

  return (
    <NotificationContainer>
      <CardHeader>
        <CardTitle>Notifications</CardTitle>
        <Button $size="small" onClick={handleClearNotifications}>
          Clear
        </Button>
      </CardHeader>

      <CardContent>
        {allNotifications.length === 0 ? (
          <EmptyState>No notifications yet</EmptyState>
        ) : (
          <NotificationList>
            {allNotifications.map((notification, index) => (
              <NotificationItem key={index} type={notification.type}>
                <NotificationHeader>
                  <NotificationTitle>
                    {notification.type === "PersonDetected"
                      ? "Person Detected"
                      : notification.type}
                  </NotificationTitle>
                  <NotificationTime>
                    {formatTime(notification.timestamp)}
                  </NotificationTime>
                </NotificationHeader>

                <NotificationMessage>
                  {notification.message}
                </NotificationMessage>

                {notification.source === "detection" &&
                  (notification as any).detections && (
                    <DetectionDetails>
                      {(notification as any).detections.length} detection(s)
                      with average confidence{" "}
                      {Math.round(
                        ((notification as any).detections.reduce(
                          (sum: number, det: any) => sum + det.confidence,
                          0,
                        ) /
                          (notification as any).detections.length) *
                          100,
                      )}
                      %
                    </DetectionDetails>
                  )}
              </NotificationItem>
            ))}
          </NotificationList>
        )}
      </CardContent>
    </NotificationContainer>
  );
};
