import styled, { css } from 'styled-components'
import { theme } from '../../styles/theme'

interface ButtonProps {
  $variant?: 'primary' | 'secondary' | 'danger' | 'success'
  $size?: 'small' | 'medium' | 'large'
  $fullWidth?: boolean
}

const getVariantStyles = (variant: string) => {
  switch (variant) {
    case 'primary':
      return css`
        background-color: ${theme.colors.primary};
        color: white;
        
        &:hover:not(:disabled) {
          background-color: ${theme.colors.primaryHover};
        }
      `
    case 'danger':
      return css`
        background-color: ${theme.colors.error};
        color: white;
        
        &:hover:not(:disabled) {
          background-color: #d60066;
        }
      `
    case 'success':
      return css`
        background-color: ${theme.colors.success};
        color: white;
        
        &:hover:not(:disabled) {
          background-color: #00a068;
        }
      `
    default:
      return css`
        background-color: ${theme.colors.surface};
        color: ${theme.colors.text};
        border: 1px solid ${theme.colors.border};
        
        &:hover:not(:disabled) {
          background-color: ${theme.colors.surfaceAlt};
        }
      `
  }
}

const getSizeStyles = (size: string) => {
  switch (size) {
    case 'small':
      return css`
        padding: ${theme.sizes.spacing.xs} ${theme.sizes.spacing.sm};
        font-size: 12px;
        min-width: 60px;
      `
    case 'large':
      return css`
        padding: ${theme.sizes.spacing.md} ${theme.sizes.spacing.lg};
        font-size: 16px;
        min-width: 120px;
      `
    default:
      return css`
        padding: ${theme.sizes.spacing.sm} ${theme.sizes.spacing.md};
        font-size: 14px;
        min-width: 80px;
      `
  }
}

export const Button = styled.button<ButtonProps>`
  border-radius: ${theme.sizes.borderRadius};
  font-weight: 600;
  transition: all ${theme.transitions.normal};
  border: none;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: ${theme.sizes.spacing.sm};
  
  ${props => getVariantStyles(props.$variant || 'secondary')}
  ${props => getSizeStyles(props.$size || 'medium')}
  
  ${props => props.$fullWidth && css`
    width: 100%;
  `}
  
  &:disabled {
    background-color: ${theme.colors.surfaceAlt};
    color: ${theme.colors.textSecondary};
    cursor: not-allowed;
    opacity: 0.6;
  }
  
  &:focus-visible {
    outline: 2px solid ${theme.colors.primary};
    outline-offset: 2px;
  }
`