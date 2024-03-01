import React from "react";
import ToolWindow from "./_tool-window";
import "./_toolbar-buttons";

if (!window.$_findStuff_cache)
    window.$_findStuff_cache = {};

const FSWindow = ({ react, setupController }) => {
    const { model, update, trigger, _L } = setupController();

    return <ToolWindow type="default" model={model} update={update} trigger={trigger} _L={_L} />;
};

window.$_gooee.register("findstuff", "FindStuffWindow", FSWindow, "main-container-end", "findstuff");


const FSSideWindow = ({ react, setupController }) => {
    const { model, update, trigger, _L } = setupController();

    return <ToolWindow type="side" model={model} update={update} trigger={trigger} _L={_L} />;
};

window.$_gooee.register("findstuff", "FindStuffSideWindow", FSSideWindow, "default", "findstuff");
