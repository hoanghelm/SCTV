import React from "react";
import { Provider } from "react-redux";
import { ThemeProvider } from "styled-components";
import { store } from "./store";
import { theme } from "./styles/theme";
import { GlobalStyles } from "./styles/GlobalStyles";
import { Dashboard } from "./components/dashboard/Dashboard";

function App() {
  return (
    <Provider store={store}>
      <ThemeProvider theme={theme}>
        <GlobalStyles />
        <Dashboard />
      </ThemeProvider>
    </Provider>
  );
}

export default App;
