import React from "react";

export function EditCode(props: { tabCodeEntry?: string }) {
  const { tabCodeEntry } = {
    tabCodeEntry: "tabs/src/index.tsx",
    ...props,
  };
  return (
    <div>
      <h2>Change this code</h2>
      <p>
        The front end is a <code>create-react-app</code>. The entry point is{" "}
        <code>{tabCodeEntry}</code>. Just save any file and this page will reload automatically.
      </p>
    </div>
  );
}
