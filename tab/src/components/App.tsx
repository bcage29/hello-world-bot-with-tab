import React, {useEffect} from "react";
// https://fluentsite.z22.web.core.windows.net/quick-start
import { FluentProvider, teamsLightTheme, tokens } from "@fluentui/react-components";
import { HashRouter as Router, Navigate, Route, Routes } from "react-router-dom";
import Privacy from "./Privacy";
import TermsOfUse from "./TermsOfUse";
import Tab from "./Tab";
import TabConfig from "./TabConfig";
import { useTeams } from "@microsoft/teamsfx-react";
import { app } from "@microsoft/teams-js";

/**
 * The main app which handles the initialization and routing
 * of the app.
 */
export default function App() {
  const { theme } = useTeams({})[0];
  useEffect(() => {
    app.initialize().then(() => {
      // Hide the loading indicator.
      app.notifySuccess();
    });
  }, []);
  return (
    <FluentProvider
      theme={
        theme || {
          ...teamsLightTheme,
          colorNeutralBackground3: "#eeeeee",
        }
      }
      style={{ background: tokens.colorNeutralBackground3 }}
    >
      <Router>
        <Routes>
          <Route path="/privacy" element={<Privacy />} />
          <Route path="/termsofuse" element={<TermsOfUse />} />
          <Route path="/tab" element={<Tab />} />
          <Route path="/config" element={<TabConfig />} />
          <Route path="*" element={<Navigate to={"/tab"} />}></Route>
        </Routes>
      </Router>
    </FluentProvider>
  );
}
