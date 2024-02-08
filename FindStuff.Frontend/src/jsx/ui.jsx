import React from "react";
import HoverWindow from "./_hover-window";
import FiltersWindow from "./_filters-window";
import SubFilters from "./_sub-filters";
import PrefabItem from "./_prefab-item";
import LoadingScreen from "./_loading-screen";
import FavouriteStar from "./_favourite_star";

const AppButton = ({ react, setupController }) => {
    const [tooltipVisible, setTooltipVisible] = react.useState(false);

    const onMouseEnter = () => {
        setTooltipVisible(true);
        engine.trigger("audio.playSound", "hover-item", 1);
    };

    const onMouseLeave = () => {
        setTooltipVisible(false);
    };

    const { ToolTip, ToolTipContent } = window.$_gooee.framework;

    const { model, update, trigger } = setupController();

    const onClick = () => {
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
    return <>
        <div className="spacer_oEi"></div>
        <button onMouseEnter={onMouseEnter} onMouseLeave={onMouseLeave} onClick={onClick} className={"button_s2g button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT button_s2g button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT toggle-states_X82 toggle-states_DTm" + (model.IsVisible ? " selected" : "")}>

            <div className="fa fa-solid-magnifying-glass icon-lg"></div>

            <ToolTip visible={tooltipVisible} float="up" align="right">
                <ToolTipContent title="Test" description="Hello, world!" />
            </ToolTip>
        </button>
    </>;
};

window.$_gooee.register("findstuff", "FindStuffAppButton", AppButton, "bottom-right-toolbar", "findstuff");

if (!window.$_findStuff_cache)
    window.$_findStuff_cache = {};
;

const debounce = (func, wait) => {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
};

const ToolWindow = ({ react, setupController }) => {
    const [sliderValue, setSliderValue] = react.useState(0);
    const [selectedPrefab, setSeletedPrefab] = react.useState({ Name: "" });
    const [hoverPrefab, setHoverPrefab] = react.useState({ Name: "" });

    const { Button, Icon, VirtualList, Slider, List, Grid, FormGroup, FormCheckBox, Scrollable, ToolTip, TextBox, Dropdown, ToolTipContent, TabModal, Modal, MarkDown } = window.$_gooee.framework;

    const { model, update, trigger, _L } = setupController();
    const [filteredPrefabs, setFilteredPrefabs] = react.useState([]);
    const [search, setSearch] = react.useState(model.Search ?? "");
    const [expanded, setExpanded] = react.useState(false);
    const [shifted, setShifted] = react.useState(false);
    const [mouseOverItem, setMouseOverItem] = react.useState(null);    

    const triggerResultsUpdate = debounce((curQueryKey, m) => {
        //if (queryKey !== curQueryKey) {
        console.log("query key: " + curQueryKey);
            // If the local JS cache has a store use that instead but only for non-searches
            if ((!m.Search || m.Search.length == 0) && window.$_findStuff_cache[curQueryKey]) {
                console.log("Got cache for " + curQueryKey);
                setFilteredPrefabs(window.$_findStuff_cache[curQueryKey]);
            }
            // Otherwise use C# backend to query it
            else {
                trigger("OnUpdateQuery");
            }
        //}
    }, 50);

    const onReceiveResults = (curQueryKey, json) => {
        const result = json ? JSON.parse(json) : null;

        if (!result || !result.Prefabs)
            return;

        setFilteredPrefabs(result.Prefabs);

        if (curQueryKey &&  curQueryKey.includes("::")) {
            window.$_findStuff_cache[curQueryKey] = result.Prefabs;
            console.log("Updated cache for " + curQueryKey);
        }
    };

    const onSelectAsset = (entity) => {
        setShifted(true);
    };
    const onSelectTool = (tool) => {
        console.log(tool);
        setShifted(tool.id !== "Default Tool");
    };

    react.useEffect(() => {
        const eventHandle = engine.on("findstuff.onReceiveResults", onReceiveResults);
        const selectAssetHandle = engine.on("toolbar.selectAsset", onSelectAsset);
        const selectToolHandle = engine.on("tool.activeTool.update", onSelectTool);

        return () => {
            eventHandle.clear();
            selectAssetHandle.clear();
            selectToolHandle.clear();
        };
    }, [model.ViewMode, model.Filter, model.SubFilter, model.Search, model.OrderByAscending, shifted])
    
    react.useEffect(() => {
        doResultsUpdate(model);
    }, []);

    const doResultsUpdate = (m) => {
        const curQueryKey = `${m.Filter}:${m.SubFilter}:${m.Search ? m.Search : ""}:${m.OrderByAscending}${m.Filter === "Favourite" ? ":" + m.Favourites.length : ""}`;
        triggerResultsUpdate(curQueryKey, model);
    };

    const debouncedSearchUpdate = debounce((val) => {
        model.Search = val;
        update("Search", val);
        doResultsUpdate(model);
    }, filteredPrefabs.length > 5_000 ? 500 : 50);

    const onSearchInputChanged = (val) => {
        setSearch(val);
        debouncedSearchUpdate(val);
    };

    const closeModal = () => {
        trigger("OnToggleVisible");
        engine.trigger("audio.playSound", "close-panel", 1);
    };

    const isVisibleClass = "tool-layout";

    const onSelectPrefab = (prefab) => {
        setSeletedPrefab(prefab);
    };

    const onMouseEnterItem = (prefab) => {
        setHoverPrefab(prefab);
        setMouseOverItem(prefab);
    };

    const onMouseLeave = () => {
        setHoverPrefab(null);
    }

    const onMouseLeaveItem = () => {
        setMouseOverItem(null);
    };

    const onUpdateFavourite = (prefabName) => {
        if (model.Favourites.includes(prefabName))
            model.Favourites = model.Favourites.filter(f => f !== prefabName);
        else
            model.Favourites.push(prefabName);
        trigger("OnToggleFavourite", prefabName);

        const curQueryKey = `${model.Filter}:${model.SubFilter}:${model.Search ? model.Search : ""}:${model.OrderByAscending}`;

        window.$_findStuff_cache[curQueryKey] = null;
    };

    const onRenderItem = (p, index) => {
        return <PrefabItem key={p.Name} model={model} trigger={trigger} selected={selectedPrefab}
            prefab={p}
            onSelected={onSelectPrefab} onMouseEnter={onMouseEnterItem} onMouseLeave={onMouseLeaveItem}
            _L={_L} extraContent={hoverPrefab && hoverPrefab.Name === p.Name ? <FavouriteStar model={model} onUpdateFavourite={onUpdateFavourite} prefab={p} /> : null} />;
    };

    const clearSearch = () => {
        setSearch("");
        model.Search = "";
        update("Search", "");
        doResultsUpdate(model);
    };

    const toggleExpander = react.useCallback(() => {
        const newValue = !expanded;
        setExpanded(newValue);
    }, [expanded, setExpanded]);

    const toggleShifter = react.useCallback(() => {
        const newValue = !shifted;
        setShifted(newValue);
    }, [shifted, setShifted]);

    const getGridCounts = () => {
        let rowsCount = 0;
        let columnsCount = 0;

        switch (model.ViewMode) {
            case "Rows":
            case "Detailed":
            case "Columns":
                columnsCount = 1;
                if (expanded) {
                    if (shifted) {
                        rowsCount = 4;
                    }
                    else {
                        rowsCount = 8;
                    }
                }
                else {
                    if (shifted) {
                        rowsCount = 2;
                    }
                    else {
                        rowsCount = 4;
                    }
                }
                break;

            case "IconGrid":
                if (expanded) {
                    if (shifted) {
                        rowsCount = 4;
                        columnsCount = 9;
                    }
                    else {
                        rowsCount = 6;
                        columnsCount = 13;
                    }
                }
                else {
                    if (shifted) {
                        rowsCount = 1;
                        columnsCount = 9;
                    }
                    else {
                        rowsCount = 3;
                        columnsCount = 13;
                    }
                }
                break;

            case "IconGridLarge":
                if (expanded) {
                    if (shifted) {
                        rowsCount = 4;
                        columnsCount = 9;
                    }
                    else {
                        rowsCount = 4;
                        columnsCount = 9;
                    }
                }
                else {
                    if (shifted) {
                        rowsCount = 1;
                        columnsCount = 9;
                    }
                    else {
                        rowsCount = 2;
                        columnsCount = 9;
                    }
                }
                break;
        }

        return { r: rowsCount, c: columnsCount };
    };

    const gridCounts = getGridCounts();
    const isBorderedList = model.ViewMode === "IconGrid" || model.ViewMode === "IconGridLarge" ? null : true;
    const columnCount = gridCounts.c;// gridCounts.model.ViewMode === "Rows" || model.ViewMode === "Detailed" ? 1 : model.ViewMode === "Columns" ? 2 : model.ViewMode === "IconGrid" ? shifted ? 9 : 13 : shifted ? 6 : 9;
    const rowCount = gridCounts.r;// model.ViewMode === "Rows" || model.ViewMode === "Detailed" || model.ViewMode === "Columns" ? (expanded ? shifted ? 4 : 8 : shifted ? 2 : 4) : model.ViewMode === "IconGrid" ? (expanded ? shifted ? 2 : 6 : shifted ? 1 : 3) : (expanded ? shifted ? 2 : 4 : shifted ? 1 : 2);

    const renderHoverWindow = () => {
        return hoverPrefab && hoverPrefab.Name && hoverPrefab.Name.length > 0 ? <HoverWindow className={shifted ? "mt-2" : "mb-2"} hoverPrefab={hoverPrefab} _L={_L} /> : null;
    };

    return model.IsVisible ? <div className={isVisibleClass + (shifted? " align-items-start" : "")}>
        <div className="col">
            <FiltersWindow compact={shifted} model={model} update={update} onDoUpdate={doResultsUpdate} _L={_L} />
        </div>
        <div className="col">
            {!shifted ? renderHoverWindow() : null}
            <Modal bodyClassName={"asset-menu p-relative" + (expanded ? " asset-menu-xl" : shifted ? " asset-menu-sm" : "")} title={<div className="d-flex flex-row align-items-center">
                <Button watch={[expanded]} circular icon style="trans-faded" onClick={toggleExpander}>
                    <Icon icon={expanded ? (!shifted ? "solid-chevron-down" : "solid-chevron-up") : (shifted ? "solid-chevron-down" : "solid-chevron-up")} fa />
                </Button>
                <Icon icon="solid-magnifying-glass" fa className="bg-muted ml-2" />
                <TextBox size="sm" className="bg-dark-trans-less-faded w-25 mr-2 ml-4" placeholder="Search..." text={search} onChange={onSearchInputChanged} />
                {<Button circular icon style="trans-faded" disabled={search && search.length > 0 ? null : true} onClick={clearSearch}>
                    <Icon icon="solid-eraser" fa />
                </Button>}
                <SubFilters model={model} update={update} onDoUpdate={doResultsUpdate} _L={_L} />
                
            </div>} onClose={closeModal}>
                <div className="asset-menu-container" onMouseLeave={() => onMouseLeave()}>
                    <div className="flex-1">
                        <VirtualList watch={[model.Favourites, selectedPrefab, mouseOverItem, model.Search, model.Filter, model.SubFilter, model.ViewMode, model.OrderByAscending]} border={isBorderedList} data={filteredPrefabs} onRenderItem={onRenderItem} columns={columnCount} rows={rowCount} contentClassName="d-flex flex-row flex-wrap" size="sm" itemHeight={32}>
                        </VirtualList>
                    </div>
                </div>
            </Modal>
            {shifted ? renderHoverWindow() : null}
        </div>
        <div className="col">
            <div className="d-inline h-x w-x">
                {/*<Button watch={[shifted]} circular border icon style="trans-faded" onClick={toggleShifter}>*/}
                {/*    <Icon icon={shifted ? "solid-arrow-down" : "solid-arrow-up"} size="sm" fa />*/}
                {/*</Button>*/}
            </div>
        </div>
    </div> : null;
};

window.$_gooee.register("findstuff", "FindStuffWindow", ToolWindow, "main-container", "findstuff");
