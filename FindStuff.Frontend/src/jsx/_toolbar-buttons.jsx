import React from "react";

const AppButton = ({ react, setupController }) => {
    const onMouseEnter = () => {
        engine.trigger("audio.playSound", "hover-item", 1);
    };    

    const { AutoToolTip, ToolTipContent } = window.$_gooee.framework;

    const { model, update, trigger, _L } = setupController();

    const onFindStuffClick = () => {
        const newValue = !model.IsVisible;
        trigger("OnToggleVisible");
        engine.trigger("audio.playSound", "select-item", 1);

        if (newValue) {
            engine.trigger("audio.playSound", "open-panel", 1);
            //engine.trigger("tool.selectTool", null);
        }
        else
            engine.trigger("audio.playSound", "close-panel", 1);
    };

    const onPickerClick = () => {
        const newValue = !model.IsPicking;
        trigger("OnTogglePicker");
        engine.trigger("audio.playSound", "select-item", 1);

        if (newValue) {
            engine.trigger("audio.playSound", "open-panel", 1);
        }
        else
            engine.trigger("audio.playSound", "close-panel", 1);
    };

    const pickerBtnRef = react.useRef(null);
    const findStuffBtnRef = react.useRef(null);

    return <>
        <div className="spacer_oEi"></div>
        <button ref={pickerBtnRef} onMouseEnter={onMouseEnter} onClick={onPickerClick} className={"button_s2g button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT button_s2g button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT toggle-states_X82 toggle-states_DTm" + (model.IsPicking ? " selected" : "")}>
            <div className="fa fa-solid-eye-dropper icon-md"></div>
            <AutoToolTip targetRef={pickerBtnRef} float="up" align="right">
                <ToolTipContent title="Pick Stuff" description="Hover over items and select them automatically." />
            </AutoToolTip>
        </button>
        <div className="spacer_oEi"></div>
        <button ref={findStuffBtnRef} onMouseEnter={onMouseEnter} onClick={onFindStuffClick} className={"button_s2g button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT button_s2g button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT toggle-states_X82 toggle-states_DTm" + (model.IsVisible ? " selected" : "")}>
            <div className="fa fa-solid-magnifying-glass icon-md"></div>
            <AutoToolTip targetRef={findStuffBtnRef} float="up" align="right">
                <ToolTipContent title={_L("FindStuff.FindStuffSettings.ModName")} description="Find stuff you can place in the game." />
            </AutoToolTip>
        </button>
    </>;
};

window.$_gooee.register("findstuff", "FindStuffAppButton", AppButton, "bottom-right-toolbar", "findstuff");