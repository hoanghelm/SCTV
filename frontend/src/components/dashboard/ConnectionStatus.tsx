import React from "react";
import { useSelector } from "react-redux";
import styled, { css } from "styled-components";
import { theme } from "../../styles/theme";
import { RootState } from "../../store";

interface StatusContainerProps {
  $status: "connecting" | "connected" | "disconnected";
}

const getStatusStyles = (status: string) => {
  switch (status) {
    case "connected":
      return css`
        background-color: ${theme.colors.success};
        color: white;
      `;
    case "connecting":
      return css`
        background-color: ${theme.colors.warning};
        color: black;
      `;
    default:
      return css`
        background-color: ${theme.colors.error};
        color: white;
      `;
  }
};

const StatusContainer = styled.div<StatusContainerProps>`
  padding: ${theme.sizes.spacing.md};
  border-radius: ${theme.sizes.borderRadius};
  margin-bottom: ${theme.sizes.spacing.lg};
  font-weight: 500;
  text-align: center;
  font-size: 14px;

  ${(props) => getStatusStyles(props.$status)}
`;

const StatusText = styled.span`
  text-transform: capitalize;
`;

const getStatusText = (status: string) => {
  switch (status) {
    case "connected":
      return "Connected to streaming service";
    case "connecting":
      return "Connecting to streaming service...";
    default:
      return "Disconnected from streaming service";
  }
};

export const ConnectionStatus: React.FC = () => {
  const { connectionStatus } = useSelector(
    (state: RootState) => state.streaming,
  );

  return (
    <StatusContainer $status={connectionStatus}>
      <StatusText>{getStatusText(connectionStatus)}</StatusText>
    </StatusContainer>
  );
};
