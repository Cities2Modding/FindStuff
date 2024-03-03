import React from "react";

const FiltersWindow = ({ compact = null, model, update, _L, onDoUpdate }) => {
    const react = window.$_gooee.react;
    const { Icon, Button, AutoToolTip, ToolTipContent, CheckBox } = window.$_gooee.framework;

    const isVertical = model.OperationMode === "HideFindStuffSideMenu";

    const updateFilter = react.useCallback((filter) => {
        model.Filter = filter;
        update("Filter", filter);

        model.SubFilter = "None";
        update("SubFilter", "None");

        if (onDoUpdate)
            onDoUpdate(model, false);
    }, [model.Filter, model.SubFilter, model.Categories, update]);

    const updateOrderBy = react.useCallback((val) => {
        model.OrderByAscending = val;
        update("OrderByAscending", val);

        if (onDoUpdate)
            onDoUpdate(model, false);
    }, [model.OrderByAscending, update]);

    const historicalCheckboxRef = react.useRef(null);

    return <div className={"bg-panel text-light rounded-sm" + (compact ? " align-self-end w-x p-2" : " p-4") + (isVertical ? " mb-2" : "")}>
        {!compact && model.Filter == "Zones" ?
            <div className="d-flex flex-row align-items-center justify-content-center fs-tool-text mb-4">
                <div className="flex-1">{_L("FindStuff.Options.Historical")}</div>
                <div className="w-x" ref={historicalCheckboxRef}>
                    <CheckBox checked={model.IsHistorical} onToggle={() => update("IsHistorical", !model.IsHistorical)} />
                </div>
                <AutoToolTip targetRef={historicalCheckboxRef} float={isVertical ? "down" : "up"} align="center">
                    <ToolTipContent title={_L("FindStuff.Options.Historical")} description={_L("FindStuff.Options.Historical_desc")} />
                </AutoToolTip>
            </div> : null}
        {!compact ?
            <div className="d-flex flex-row align-items-center justify-content-center fs-tool-text">
                <div className="flex-1">
                    {_L("FindStuff.View")}
                </div>
                <Button title={_L("FindStuff.View.Rows")} description={_L("FindStuff.View.Rows_desc")}
                    toolTipFloat={isVertical ? "down" : "up"}
                    className={"mr-1" + (model.ViewMode === "Rows" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "Rows")}>
                    <Icon icon="solid-bars" fa />
                </Button>
                <Button title={_L("FindStuff.View.Columns")} description={_L("FindStuff.View.Columns_desc")}
                    toolTipFloat={isVertical ? "down" : "up"}
                    className={"mr-1" + (model.ViewMode === "Columns" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "Columns")}>
                    <Icon icon="solid-table-columns" fa />
                </Button>
                <Button title={_L("FindStuff.View.IconGrid")} description={_L("FindStuff.View.IconGrid_desc")}
                    toolTipFloat={isVertical ? "down" : "up"}
                    className={"mr-1" + (model.ViewMode === "IconGrid" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "IconGrid")}>
                    <Icon icon="solid-table-cells" fa />
                </Button>
                <Button title={_L("FindStuff.View.IconGridLarge")} description={_L("FindStuff.View.IconGridLarge_desc")}
                    toolTipFloat={isVertical ? "down" : "up"}
                    className={"mr-1" + (model.ViewMode === "IconGridLarge" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "IconGridLarge")}>
                    <Icon icon="solid-table-cells-large" fa />
                </Button>
                <Button title={_L("FindStuff.View.Detailed")} description={_L("FindStuff.View.Detailed_desc")}
                    toolTipFloat={isVertical ? "down" : "up"}
                    className={"" + (model.ViewMode === "Detailed" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "Detailed")}>
                    <Icon icon="solid-table-list" fa />
                </Button>
            </div> : null}
        <div className={"d-flex flex-row align-items-center justify-content-center fs-tool-text" + (!compact ? " mt-4" : "")}>
            {!compact ? <div className="flex-1">
                {_L("FindStuff.Filter")}
            </div> : null}
            <div className={compact ? "w-x" : null}>
                <div className="d-flex flex-row flex-wrap align-items-center justify-content-end">
                    <Button title={_L("FindStuff.Filter.None")} description={_L("FindStuff.Filter.None_desc")}
                        toolTipFloat={isVertical ? "down" : "up"}
                        className={(!model.Filter || model.Filter === "None" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("None")}>
                        <Icon icon="solid-asterisk" fa />
                    </Button>
                    <Button title={_L("FindStuff.Filter.Favourite")} description={_L("FindStuff.Filter.Favourite_desc")}
                        toolTipFloat={isVertical ? "down" : "up"}
                        className={"ml-1" + (model.Filter === "Favourite" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Favourite")}>
                        <Icon icon="solid-star" fa />
                    </Button>
                    <Button title={_L("FindStuff.Filter.Foliage")} description={_L("FindStuff.Filter.Foliage_desc")}
                        toolTipFloat={isVertical ? "down" : "up"}
                        className={"ml-1" + (model.Filter === "Foliage" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Foliage")}>
                        <Icon icon="Media/Game/Icons/Forest.svg" />
                    </Button>
                    <Button title={_L("FindStuff.Filter.Network")} description={_L("FindStuff.Filter.Network_desc")}
                        toolTipFloat={isVertical ? "down" : "up"}
                        className={"ml-1" + (model.Filter === "Network" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Network")}>
                        <Icon icon="Media/Game/Icons/Roads.svg" />
                    </Button>
                </div>
                <div className="d-flex flex-row flex-wrap align-items-center justify-content-end mt-1">
                    <Button title={_L("FindStuff.Filter.Buildings")} description={_L("FindStuff.Filter.Buildings_desc")}
                        toolTipFloat={isVertical ? "down" : "up"}
                        className={(model.Filter === "Buildings" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Buildings")}>
                        <Icon icon="Media/Game/Icons/ZoneSignature.svg" />
                    </Button>
                    <Button title={_L("FindStuff.Filter.Zones")} description={_L("FindStuff.Filter.Zones_desc")}
                        toolTipFloat={isVertical ? "down" : "up"}
                        className={"ml-1" + (model.Filter === "Zones" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Zones")}>
                        <Icon icon="Media/Game/Icons/Zones.svg" />
                    </Button>
                    <Button title={_L("FindStuff.Filter.Props")} description={_L("FindStuff.Filter.Props_desc")}
                        toolTipFloat={isVertical ? "down" : "up"}
                        className={"ml-1" + (model.Filter === "Props" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Props")}>
                        <Icon icon="solid-cube" fa />
                    </Button>
                    <Button title={_L("FindStuff.Filter.Misc")} description={_L("FindStuff.Filter.Misc_desc")}
                        toolTipFloat={isVertical ? "down" : "up"}
                        className={"ml-1" + (model.Filter === "Misc" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Misc")}>
                        <Icon icon="solid-ellipsis" fa />
                    </Button>
                </div>
            </div>
        </div>
        {!compact ?
            <div className="d-flex flex-row align-items-center justify-content-center mt-4 fs-tool-text">
                <div className="flex-1">
                    {_L("FindStuff.OrderBy")}
                </div>
                <div>
                    <div className="d-flex flex-row flex-wrap align-items-center justify-content-end">
                        <Button disabled={model.Search && model.Search.length > 0}
                            title={_L("FindStuff.OrderBy.Ascending")} description={_L("FindStuff.OrderBy.Ascending_desc")}
                            toolTipFloat={isVertical ? "down" : "up"}
                            className={(model.OrderByAscending === true ? " active" : "")} color="tool" size="sm" icon onClick={() => updateOrderBy(true)}>
                            <Icon icon="solid-arrow-down-a-z" fa />
                        </Button>
                        <Button disabled={model.Search && model.Search.length > 0}
                            title={_L("FindStuff.OrderBy.Descending")} description={_L("FindStuff.OrderBy.Descending_desc")}
                            toolTipFloat={isVertical ? "down" : "up"}
                            className={"ml-1" + (model.OrderByAscending === false ? " active" : "")} color="tool" size="sm" icon onClick={() => updateOrderBy(false)}>
                            <Icon icon="solid-arrow-up-a-z" fa />
                        </Button>
                    </div>
                </div>
            </div> : null}
    </div>;
};

export default FiltersWindow;