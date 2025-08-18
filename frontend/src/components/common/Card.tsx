import styled from 'styled-components'
import { theme } from '../../styles/theme'

export const Card = styled.div`
  background-color: ${theme.colors.surface};
  border-radius: ${theme.sizes.borderRadiusLarge};
  padding: ${theme.sizes.spacing.lg};
  box-shadow: ${theme.shadows.card};
  border: 1px solid ${theme.colors.border};
`

export const CardHeader = styled.div`
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: ${theme.sizes.spacing.md};
  padding-bottom: ${theme.sizes.spacing.md};
  border-bottom: 1px solid ${theme.colors.border};
`

export const CardTitle = styled.h3`
  font-size: 18px;
  font-weight: 600;
  color: ${theme.colors.text};
  margin: 0;
`

export const CardContent = styled.div`
  color: ${theme.colors.text};
`