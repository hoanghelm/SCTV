import styled, { css } from 'styled-components'
import { theme } from '../../styles/theme'

interface InputProps {
  $variant?: 'default' | 'error' | 'success'
  $size?: 'small' | 'medium' | 'large'
  $fullWidth?: boolean
}

const getVariantStyles = (variant: string) => {
  switch (variant) {
    case 'error':
      return css`
        border-color: ${theme.colors.error};
        &:focus {
          border-color: ${theme.colors.error};
          box-shadow: 0 0 0 2px rgba(249, 24, 128, 0.2);
        }
      `
    case 'success':
      return css`
        border-color: ${theme.colors.success};
        &:focus {
          border-color: ${theme.colors.success};
          box-shadow: 0 0 0 2px rgba(0, 186, 124, 0.2);
        }
      `
    default:
      return css`
        border-color: ${theme.colors.border};
        &:focus {
          border-color: ${theme.colors.primary};
          box-shadow: 0 0 0 2px rgba(29, 155, 240, 0.2);
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
        height: 32px;
      `
    case 'large':
      return css`
        padding: ${theme.sizes.spacing.md} ${theme.sizes.spacing.lg};
        font-size: 16px;
        height: 48px;
      `
    default:
      return css`
        padding: ${theme.sizes.spacing.sm} ${theme.sizes.spacing.md};
        font-size: 14px;
        height: 40px;
      `
  }
}

export const Input = styled.input<InputProps>`
  border-radius: ${theme.sizes.borderRadius};
  border: 1px solid;
  background-color: ${theme.colors.background};
  color: ${theme.colors.text};
  font-family: ${theme.fonts.main};
  transition: all ${theme.transitions.normal};
  outline: none;
  
  ${props => getVariantStyles(props.$variant || 'default')}
  ${props => getSizeStyles(props.$size || 'medium')}
  
  ${props => props.$fullWidth && css`
    width: 100%;
  `}
  
  &::placeholder {
    color: ${theme.colors.textSecondary};
  }
  
  &:disabled {
    background-color: ${theme.colors.surfaceAlt};
    color: ${theme.colors.textSecondary};
    cursor: not-allowed;
    opacity: 0.6;
  }
`

export const Select = styled.select<InputProps>`
  border-radius: ${theme.sizes.borderRadius};
  border: 1px solid;
  background-color: ${theme.colors.background};
  color: ${theme.colors.text};
  font-family: ${theme.fonts.main};
  transition: all ${theme.transitions.normal};
  outline: none;
  cursor: pointer;
  
  ${props => getVariantStyles(props.$variant || 'default')}
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
  
  option {
    background-color: ${theme.colors.background};
    color: ${theme.colors.text};
  }
`

export const Label = styled.label`
  font-weight: 500;
  color: ${theme.colors.text};
  font-size: 14px;
  margin-bottom: ${theme.sizes.spacing.xs};
  display: block;
`

export const FormGroup = styled.div`
  display: flex;
  flex-direction: column;
  gap: ${theme.sizes.spacing.xs};
  margin-bottom: ${theme.sizes.spacing.md};
`

export const FormRow = styled.div`
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: ${theme.sizes.spacing.md};
  align-items: end;
  
  @media (max-width: 600px) {
    grid-template-columns: 1fr;
  }
`