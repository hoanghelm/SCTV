import styled, { css } from "styled-components";
import { theme } from "../../styles/theme";

interface StatusBadgeProps {
  $status:
    | "connected"
    | "connecting"
    | "disconnected"
    | "error"
    | "active"
    | "inactive"
    | "reconnecting";
}

const getStatusStyles = (status: string) => {
  switch (status) {
    case "connected":
    case "active":
      return css`
        background-color: ${theme.colors.success};
        color: white;
      `;
    case "connecting":
    case "reconnecting":
      return css`
        background-color: ${theme.colors.warning};
        color: black;
      `;
    case "error":
      return css`
        background-color: ${theme.colors.error};
        color: white;
      `;
    default:
      return css`
        background-color: ${theme.colors.surfaceAlt};
        color: ${theme.colors.textSecondary};
      `;
  }
};

export const StatusBadge = styled.span<StatusBadgeProps>`
  font-size: 12px;
  padding: ${theme.sizes.spacing.xs} ${theme.sizes.spacing.sm};
  border-radius: ${theme.sizes.borderRadius};
  font-weight: 500;
  text-transform: capitalize;

  ${(props) => getStatusStyles(props.$status)}
`;
