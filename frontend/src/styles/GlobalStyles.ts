import { createGlobalStyle } from "styled-components";
import { theme } from "./theme";

export const GlobalStyles = createGlobalStyle`
  * {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
  }

  body {
    font-family: ${theme.fonts.main};
    background-color: ${theme.colors.background};
    color: ${theme.colors.text};
    line-height: 1.5;
    overflow-x: hidden;
  }

  #root {
    min-height: 100vh;
  }

  button {
    font-family: inherit;
    border: none;
    outline: none;
    cursor: pointer;
    transition: all ${theme.transitions.normal};
  }

  input, select, textarea {
    font-family: inherit;
    outline: none;
    transition: all ${theme.transitions.normal};
  }

  a {
    color: ${theme.colors.primary};
    text-decoration: none;
    transition: color ${theme.transitions.normal};
    
    &:hover {
      color: ${theme.colors.primaryHover};
    }
  }

  ::-webkit-scrollbar {
    width: 8px;
    height: 8px;
  }

  ::-webkit-scrollbar-track {
    background: ${theme.colors.background};
  }

  ::-webkit-scrollbar-thumb {
    background: ${theme.colors.surfaceAlt};
    border-radius: 4px;
  }

  ::-webkit-scrollbar-thumb:hover {
    background: ${theme.colors.border};
  }

  .detection-box {
    position: absolute;
    border: 2px solid ${theme.colors.accent};
    background-color: ${theme.colors.accentAlt};
    pointer-events: none;
    transition: opacity ${theme.transitions.fast};
    opacity: 1;
    z-index: 10;
    
    &.fading {
      opacity: 0;
    }
  }

  .detection-label {
    position: absolute;
    top: -25px;
    left: 0;
    background-color: ${theme.colors.accent};
    color: black;
    padding: 2px 6px;
    font-size: 12px;
    font-weight: bold;
    border-radius: 3px;
    white-space: nowrap;
  }
`;
