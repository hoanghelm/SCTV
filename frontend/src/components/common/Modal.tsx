import React, { useEffect } from "react";
import styled, { keyframes } from "styled-components";
import { theme } from "../../styles/theme";

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  width?: string;
  height?: string;
}

const fadeIn = keyframes`
  from { opacity: 0; }
  to { opacity: 1; }
`;

const slideIn = keyframes`
  from { 
    opacity: 0;
    transform: translate(-50%, -50%) scale(0.9);
  }
  to { 
    opacity: 1;
    transform: translate(-50%, -50%) scale(1);
  }
`;

const ModalOverlay = styled.div<{ $isOpen: boolean }>`
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.8);
  display: ${(props) => (props.$isOpen ? "flex" : "none")};
  align-items: center;
  justify-content: center;
  z-index: 1000;
  animation: ${fadeIn} 0.2s ease-out;
`;

const ModalContent = styled.div<{ $width?: string; $height?: string }>`
  background-color: ${theme.colors.surface};
  border-radius: ${theme.sizes.borderRadiusLarge};
  box-shadow: ${theme.shadows.large};
  border: 1px solid ${theme.colors.border};
  width: ${(props) => props.$width || "500px"};
  height: ${(props) => props.$height || "auto"};
  max-width: 90vw;
  max-height: 90vh;
  position: relative;
  animation: ${slideIn} 0.2s ease-out;
  display: flex;
  flex-direction: column;
`;

const ModalHeader = styled.div`
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: ${theme.sizes.spacing.lg};
  border-bottom: 1px solid ${theme.colors.border};
`;

const ModalTitle = styled.h2`
  margin: 0;
  color: ${theme.colors.text};
  font-size: 20px;
  font-weight: 600;
`;

const ModalBody = styled.div`
  padding: ${theme.sizes.spacing.lg};
  flex: 1;
  overflow-y: auto;
`;

const CloseButton = styled.button`
  background: none;
  border: none;
  color: ${theme.colors.textSecondary};
  font-size: 24px;
  cursor: pointer;
  padding: ${theme.sizes.spacing.xs};
  border-radius: ${theme.sizes.borderRadius};
  transition: all ${theme.transitions.normal};

  &:hover {
    background-color: ${theme.colors.surfaceAlt};
    color: ${theme.colors.text};
  }
`;

export const Modal: React.FC<ModalProps> = ({
  isOpen,
  onClose,
  title,
  children,
  width,
  height,
}) => {
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        onClose();
      }
    };

    if (isOpen) {
      document.addEventListener("keydown", handleEscape);
      document.body.style.overflow = "hidden";
    }

    return () => {
      document.removeEventListener("keydown", handleEscape);
      document.body.style.overflow = "unset";
    };
  }, [isOpen, onClose]);

  const handleOverlayClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  return (
    <ModalOverlay $isOpen={isOpen} onClick={handleOverlayClick}>
      <ModalContent $width={width} $height={height}>
        <ModalHeader>
          <ModalTitle>{title}</ModalTitle>
          <CloseButton onClick={onClose}>×</CloseButton>
        </ModalHeader>
        <ModalBody>{children}</ModalBody>
      </ModalContent>
    </ModalOverlay>
  );
};
