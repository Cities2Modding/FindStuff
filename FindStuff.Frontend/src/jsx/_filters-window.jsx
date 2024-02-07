import React from "react";

const FiltersWindow = ({ model, update, _L, onDoUpdate }) => {
    const react = window.$_gooee.react;
    const { Icon, Button } = window.$_gooee.framework;    

    const hasSubFilter = (filter) => {
        for (let i = 0; i < model.Categories.length; i++) {
            const category = model.Categories[i];

            if (category.Filter === filter) {
                return category.SubFilters.includes(model.SubFilter);
            }
        }
        return false;
    };

    const updateFilter = react.useCallback((filter) => {
        model.Filter = filter;
        update("Filter", filter);

        if (model.SubFilter && model.Categories && !hasSubFilter(filter)) {
            model.SubFilter = "None";
            update("SubFilter", "None");
        }

        if (onDoUpdate)
            onDoUpdate(model);
    }, [model.Filter, model.SubFilter, model.Categories, update]);

    const updateOrderBy = react.useCallback((val) => {
        model.OrderByAscending = val;
        update("OrderByAscending", val);

        if (onDoUpdate)
            onDoUpdate(model);
    }, [model.OrderByAscending, update]);

    return <div className="bg-panel text-light p-4 rounded-sm">
        <div className="d-flex flex-row align-items-center justify-content-center">
            <div className="flex-1">
                {_L("FindStuff.View")}
            </div>
            <Button className={"mr-1" + (model.ViewMode === "Rows" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "Rows")}>
                <Icon icon="solid-list" fa />
            </Button>
            <Button className={"mr-1" + (model.ViewMode === "Columns" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "Columns")}>
                <Icon icon="solid-list" fa />
            </Button>
            <Button className={"mr-1" + (model.ViewMode === "IconGrid" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "IconGrid")}>
                <Icon icon="solid-table-cells" fa />
            </Button>
            <Button className={"mr-1" + (model.ViewMode === "IconGridLarge" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "IconGridLarge")}>
                <Icon icon="solid-border-all" fa />
            </Button>
            <Button className={"" + (model.ViewMode === "Detailed" ? " active" : "")} color="tool" size="sm" icon onClick={() => update("ViewMode", "Detailed")}>
                <Icon icon="solid-align-justify" fa />
            </Button>
        </div>
        <div className="d-flex flex-row align-items-center justify-content-center mt-4">
            <div className="flex-1">
                {_L("FindStuff.Filter")}
            </div>
            <div>
                <div className="d-flex flex-row flex-wrap align-items-center justify-content-end">
                    <Button className={(!model.Filter || model.Filter === "None" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("None")}>
                        <Icon icon="solid-asterisk" fa />
                    </Button>
                    <Button className={"ml-1" + (model.Filter === "Favourite" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Favourite")}>
                        <Icon icon="solid-star" fa />
                    </Button>
                    <Button className={"ml-1" + (model.Filter === "Foliage" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Foliage")}>
                        <Icon icon="Media/Game/Icons/Forest.svg" />
                    </Button>
                    <Button className={"ml-1" + (model.Filter === "Network" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Network")}>
                        <Icon icon="Media/Game/Icons/Roads.svg" />
                    </Button>
                </div>
                <div className="d-flex flex-row flex-wrap align-items-center justify-content-end mt-1">
                    <Button className={(model.Filter === "Buildings" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Buildings")}>
                        <Icon icon="Media/Game/Icons/ZoneSignature.svg" />
                    </Button>
                    <Button className={"ml-1" + (model.Filter === "Zones" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Zones")}>
                        <Icon icon="Media/Game/Icons/Zones.svg" />
                    </Button>
                    <Button className={"ml-1" + (model.Filter === "Surface" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Surface")}>
                        <Icon icon="solid-pencil" fa />
                    </Button>
                    <Button className={"ml-1" + (model.Filter === "Misc" ? " active" : "")} color="tool" size="sm" icon onClick={() => updateFilter("Misc")}>
                        <Icon icon="solid-question" fa />
                    </Button>
                </div>
            </div>
        </div>
        <div className="d-flex flex-row align-items-center justify-content-center mt-4">
            <div className="flex-1">
                {_L("FindStuff.OrderBy")}
            </div>
            <div>
                <div className="d-flex flex-row flex-wrap align-items-center justify-content-end">
                    <Button className={(model.OrderByAscending === true ? " active" : "")} color="tool" size="sm" icon onClick={() => updateOrderBy(true)}>
                        <Icon icon="solid-arrow-down-a-z" fa />
                    </Button>
                    <Button className={"ml-1" + (model.OrderByAscending === false ? " active" : "")} color="tool" size="sm" icon onClick={() => updateOrderBy(false)}>
                        <Icon icon="solid-arrow-up-a-z" fa />
                    </Button>
                </div>
            </div>
        </div>
    </div>;
};

export default FiltersWindow;