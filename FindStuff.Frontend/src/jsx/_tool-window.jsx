import React from "react";
import HoverWindow from "./_hover-window";
import FiltersWindow from "./_filters-window";
import SubFilters from "./_sub-filters";
import PrefabItem from "./_prefab-item";
import LoadingScreen from "./_loading-screen";
import FavouriteStar from "./_favourite_star";
import SearchField from "./_search-field";
import "./_toolbar-buttons";
import debounce from "lodash.debounce";

const ToolWindow = ({ type, model, update, trigger, _L }) => {
    const isVertical = model.OperationMode === "HideFindStuffSideMenu";

    if ((type === "side" && !isVertical) ||
        (type === "default" && isVertical))
        return null;

    const react = window.$_gooee.react;
    const { Button, Icon, VirtualList, Modal } = window.$_gooee.framework;

    const [hoverPrefab, setHoverPrefab] = react.useState({ Name: "" });
    const [selectedPrefab, setSeletedPrefab] = react.useState(model && model.Selected ? model.Selected : { Name: "" });
    const [filteredPrefabs, setFilteredPrefabs] = react.useState([]);
    const [expanded, setExpanded] = react.useState(false);
    const [shifted, setShifted] = react.useState(model.Shifted);
    const [mouseOverItem, setMouseOverItem] = react.useState(null);
    const [isWaitingResults, setIsWaitingResults] = react.useState(false);
    const [showLoading, setShowLoading] = react.useState(false);

    const updateAssetHide = () => {
        if (model.OperationMode == "HideAssetMenu" && model.IsVisible)
            document.body.classList.add("find-stuff-hide");
        else
            document.body.classList.remove("find-stuff-hide");
    };

    const updateShift = (turnOn) => {
        if (turnOn) {
            model.Shifted = true;
            update("Shifted", model.Shifted);
            setShifted(model.Shifted);
        }
        else {
            model.Shifted = false;
            update("Shifted", model.Shifted);
            setShifted(model.Shifted);
            updateAssetHide();
        }
    };

    react.useEffect(() => {
        if (model.IsVisible && (model.OperationMode === "HideFindStuff" || model.OperationMode === "HideFindStuffSideMenu")) {
            engine.trigger("tool.selectTool", "Default Tool");
            engine.trigger("toolbar.clearAssetSelection");
        }
        else if (!model.IsVisible) {
            updateShift(false);
        }
        else if (model.IsVisible && model.OperationMode === "MoveFindStuff" && model.Shifted) {
            setShifted(true);
        }
        else if (model.IsVisible && model.OperationMode === "HideAssetMenu") {
            updateAssetHide();
        }
    }, [model.IsVisible, model.OperationMode, shifted, model.Shifted]);

    const triggerResultsUpdateFunc = (curQueryKey, m) => {
        //if (queryKey !== curQueryKey) {
        //console.log("query key: " + curQueryKey);
        // If the local JS cache has a store use that instead but only for non-searches
        if ((!m.Search || m.Search.length == 0) && window.$_findStuff_cache[curQueryKey]) {
            // console.log("Got cache for " + curQueryKey);
            setFilteredPrefabs(window.$_findStuff_cache[curQueryKey]);
            setIsWaitingResults(false);
            setShowLoading(false);
        }
        // Otherwise use C# backend to query it
        else {
            trigger("OnUpdateQuery");
        }
        //}
    };

    const triggerResultsUpdate = debounce(triggerResultsUpdateFunc, 50);

    const onReceiveResults = (curQueryKey, json) => {
        setIsWaitingResults(false);
        setShowLoading(false);
        const result = json ? JSON.parse(json) : null;

        if (!result || !result.Prefabs)
            return;

        setFilteredPrefabs(result.Prefabs);

        if (curQueryKey && curQueryKey.includes("::")) {
            window.$_findStuff_cache[curQueryKey] = result.Prefabs;
            console.log("Updated cache for " + curQueryKey);
        }
    };

    const onSelectAsset = (entity) => {
        if (model.OperationMode === "MoveFindStuff")
            updateShift(true);
        else
            updateShift(false);

        // We should select the prefab if it's not already selected
        if (entity && entity.index >= 0 && (!selectedPrefab || entity.index != selectedPrefab.ID)) {
            trigger("OnNeedHighlightPrefab", "" + entity.index);
        }
    };

    const onSelectTool = (tool) => {
        const isDefaultTool = tool.id === "Default Tool";
        const isPickerTool = tool.id === "PickStuff";

        // Do nothing if it's the picker tool
        if (isPickerTool)
            return;

        if (model.OperationMode === "HideFindStuff" || model.OperationMode === "HideFindStuffSideMenu") {
            if (!isDefaultTool) {
                model.IsVisible = false;
                trigger("OnHide");
            }
        }
        else if (model.OperationMode === "MoveFindStuff")
            updateShift(!isDefaultTool);

        if (tool.id !== "PickStuff" && model.IsPicking) {
            model.IsPicking = false;
            update("IsPicking", false);
        }
    };

    const onShowLoader = () => {
        if (isWaitingResults)
            setShowLoading(true);
        else
            setShowLoading(false);
    };

    react.useEffect(() => {
        if (model) {
            if (model.Selected) {
                setSeletedPrefab(model.Selected);
            }
        }

        const eventHandle = engine.on("findstuff.onReceiveResults", onReceiveResults);
        const selectAssetHandle = engine.on("toolbar.selectAsset", onSelectAsset);
        const selectToolHandle = engine.on("tool.activeTool.update", onSelectTool);
        const showLoaderHandle = engine.on("findstuff.onShowLoader", onShowLoader);

        return () => {
            eventHandle.clear();
            selectAssetHandle.clear();
            selectToolHandle.clear();
            showLoaderHandle.clear();
        };
    }, [model.ViewMode, isWaitingResults, showLoading, model.Selected, model.Shifted, model.OperationMode,
    model.Filter, model.SubFilter, model.Search, model.OrderByAscending, shifted])

    const doResultsUpdate = (m, debounced) => {
        const curQueryKey = `${m.Filter}:${m.SubFilter}:${m.Search ? m.Search : ""}:${m.OrderByAscending}${m.Filter === "Favourite" ? ":" + m.Favourites.length : ""}`;

        if (debounced)
            triggerResultsUpdate(curQueryKey, model);
        else
            triggerResultsUpdateFunc(curQueryKey, model);
    };

    react.useEffect(() => {
        doResultsUpdate(model, false);
    }, [model]);

    const updateSearchBackend = () => {
        doResultsUpdate(model, true);
        setIsWaitingResults(true);
    };

    const closeModal = () => {
        trigger("OnToggleVisible");
        engine.trigger("audio.playSound", "close-panel", 1);
    };

    const isVisibleClass = "tool-layout";

    const onSelectPrefab = (prefab) => {
        setSeletedPrefab(prefab);
        model.Selected = prefab;
        update("Selected", prefab);
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

    const toggleExpander = react.useCallback(() => {
        const newValue = !expanded;
        setExpanded(newValue);
    }, [expanded, setExpanded]);

    const toggleShifter = react.useCallback(() => {
        const newValue = !shifted;
        updateShift(newValue);
    }, [shifted, setShifted]);

    const getGridCounts = () => {
        let rowsCount = 0;
        let columnsCount = 0;
        const isVertical = model.OperationMode === "HideFindStuffSideMenu";

        switch (model.ViewMode) {
            case "Rows":
            case "Detailed":
            case "Columns":
                if (isVertical) {
                    if (model.ViewMode === "Columns")
                        columnsCount = 2;
                    else
                        columnsCount = 1;

                    rowsCount = 22;
                }
                else {
                    if (model.ViewMode === "Columns")
                        columnsCount = 2;
                    else
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
                }
                break;

            case "IconGrid":
                if (isVertical) {
                    rowsCount = 17;
                    columnsCount = 8;
                }
                else {
                    if (expanded) {
                        if (shifted) {
                            rowsCount = 3;
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
                }
                break;

            case "IconGridLarge":
                if (isVertical) {
                    rowsCount = 12;
                    columnsCount = 6;
                }
                else {
                    if (expanded) {
                        if (shifted) {
                            rowsCount = 2;
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
                }
                break;
        }

        return { r: rowsCount, c: columnsCount };
    };

    const gridCounts = getGridCounts();
    const isBorderedList = model.ViewMode === "IconGrid" || model.ViewMode === "IconGridLarge" ? null : true;
    const columnCount = gridCounts.c;// gridCounts.model.ViewMode === "Rows" || model.ViewMode === "Detailed" ? 1 : model.ViewMode === "Columns" ? 2 : model.ViewMode === "IconGrid" ? shifted ? 9 : 13 : shifted ? 6 : 9;
    const rowCount = gridCounts.r;// model.ViewMode === "Rows" || model.ViewMode === "Detailed" || model.ViewMode === "Columns" ? (expanded ? shifted ? 4 : 8 : shifted ? 2 : 4) : model.ViewMode === "IconGrid" ? (expanded ? shifted ? 2 : 6 : shifted ? 1 : 3) : (expanded ? shifted ? 2 : 4 : shifted ? 1 : 2);

    const renderHoverWindow = react.useCallback(() => {
        return hoverPrefab && hoverPrefab.Name && hoverPrefab.Name.length > 0 ?
            <HoverWindow model={model} className={shifted ? "mt-2" : "mb-2"} hoverPrefab={hoverPrefab} _L={_L} />
            : null;
    }, [hoverPrefab, model, shifted, model.Search]);

    const titleContent = <div className={"d-flex flex-row align-items-center" + (isVertical ? " w-x justify-content-start flex-1" : "")}>
        {!isVertical ? <Button title={_L("FindStuff.Expand")} description={_L("FindStuff.Expand_desc")} watch={[expanded]} circular icon style="trans-faded" onClick={toggleExpander}>
            <Icon icon={expanded ? (!shifted ? "solid-chevron-down" : "solid-chevron-up") : (shifted ? "solid-chevron-down" : "solid-chevron-up")} fa />
        </Button> : null}
        <Icon icon="solid-magnifying-glass" fa className={"bg-muted" + (!isVertical ? " ml-2" : "")} />
        <SearchField model={model} _L={_L} showLoading={showLoading} className="ml-4" updateModel={update} onUpdate={updateSearchBackend} debounceDelay={filteredPrefabs.length > 5_000 ? 500 : 150} />
        {isVertical ? null : <SubFilters model={model} update={update} onDoUpdate={doResultsUpdate} _L={_L} />}
    </div>;

    const modalContent = <>
        <div className="asset-menu-container" onMouseLeave={() => onMouseLeave()}>
            <div className="flex-1 p-relative">
                <VirtualList watch={[model.Favourites, selectedPrefab, mouseOverItem, model.Search, model.Filter, model.SubFilter, model.ViewMode, model.OrderByAscending]}
                    border={isBorderedList} data={filteredPrefabs} onRenderItem={onRenderItem} columns={columnCount} rows={rowCount}
                    contentClassName={"d-flex flex-row flex-wrap" + (showLoading ? " opacity-50" : "")} size="sm" itemHeight={32}>
                </VirtualList>
                <LoadingScreen isVisible={showLoading} />
            </div>
        </div>
    </>;

    return model.IsVisible ? (model.OperationMode !== "HideFindStuffSideMenu" ? <div className={isVisibleClass + (shifted ? " align-items-start" : "")}>
        <div className="col">
            <FiltersWindow compact={shifted} model={model} update={update} onDoUpdate={doResultsUpdate} _L={_L} />
        </div>
        <div className="col">
            {!shifted ? renderHoverWindow() : null}
            <Modal bodyClassName={"asset-menu p-relative" + (shifted && expanded ? "" : expanded ? " asset-menu-xl" : shifted ? " asset-menu-sm" : "")}
                title={titleContent} onClose={closeModal}>
                {modalContent}
            </Modal>
            {shifted ? renderHoverWindow() : null}
        </div>
        <div className="col">
        </div>
    </div> : <div className="p-fixed p-right-0 p-top-0 vw-50 p-2">
            <div className="d-flex flex-row align-items-start justify-content-end w-x">
                <div className="vw-20">
                    <FiltersWindow compact={shifted} model={model} update={update} onDoUpdate={doResultsUpdate} _L={_L} />
                    {renderHoverWindow()}
                </div>
                <Modal bodyClassName="p-0" className="ml-2 w-x"
                    title={titleContent} onClose={closeModal}>
                    <SubFilters model={model} update={update} onDoUpdate={doResultsUpdate} _L={_L} />
                    <div className="flex-1 w-x asset-menu asset-menu-vertical m-2">
                        {modalContent}
                    </div>
                </Modal>
            </div>
        </div>) : null;
};

export default ToolWindow;