import React from "react";

const PrefabItem = ({ model, trigger, prefab, selected, _L, onSelected, onMouseEnter, onMouseLeave, extraContent }) => {
    const react = window.$_gooee.react;
    const { Icon, Grid, Button } = window.$_gooee.framework;

    const isFAIcon = prefab.TypeIcon.includes("fa:");
    const iconSrc = react.useMemo(() => isFAIcon ? prefab.TypeIcon.replace("fa:", "") : prefab.TypeIcon, [prefab.TypeIcon]);
    
    const prefabName = () => {
        const key = `Assets.NAME[${prefab.Name}]`;
        const name = _L(key);

        if (name === key)
            return prefab.Name;
        else
            return name;
    };
    
    const computedPrefabName = react.useMemo(() => prefabName(), [prefab.Name, _L]);

    const highlightSearchTerm = (text) => {
        const regex = new RegExp(`(${model.Search})`, 'gi');
        const splitText = text.split(regex);

        return (!model.Search || model.Search.length == 0) ? text : splitText.map((part, index) =>
            regex.test(part) ? <span key={index}>
                <b className={selected.Name == prefab.Name ? "text-dark bg-warning" : "text-dark bg-warning"}>{part}</b>
            </span> : part
        );
    };

    const highlightedName = react.useMemo(() => highlightSearchTerm(computedPrefabName), [computedPrefabName, model.Search]);
    const highlightedType = react.useMemo(() => highlightSearchTerm(prefab.Type), [prefab.Type, model.Search]);

    const onSelectPrefab = () => {
        trigger("OnSelectPrefab", prefab.Name);

        if (onSelected)
            onSelected(prefab);
    };

    const onInternalMouseEnter = () => {
        if (onMouseEnter)
            onMouseEnter(prefab);
    };

    const onInternalMouseLeave = () => {
        if (onMouseLeave)
            onMouseLeave(prefab);
    };

    const render = react.useCallback(() => {
        if (model.ViewMode == "Detailed") {
            return <>
                <Grid>
                    <div className="col-7">
                        <div className="d-flex flex-row">
                            <img className="icon icon-sm ml-1 mr-1" src={prefab.Thumbnail} />
                            <span className="fs-sm flex-1">{highlightedName}</span>
                        </div>
                    </div>
                    <div className="col-2">
                        <span className="fs-xs h-x">
                            <Icon icon={iconSrc} fa={isFAIcon ? true : null} size="sm" className={(isFAIcon ? "bg-muted " : "") + "mr-1"} style={{ maxHeight: "16rem" }} />
                            {highlightedType}
                        </span>
                    </div>
                    <div className="col-2">
                        {prefab.Meta && prefab.Meta.IsDangerous ? <div className="badge badge-xs badge-danger">Dangerous</div> : null}
                    </div>
                    <div className="col-1 p-relative pr-2">
                        {extraContent ? extraContent : null}
                    </div>
                </Grid>
            </>
        }
        else {
            return <>
                <img className={model.ViewMode === "IconGrid" ? "icon icon-lg" : model.ViewMode === "IconGridLarge" ? "icon icon-xl" : "icon icon-sm ml-2"} src={prefab.Thumbnail} />
                {model.ViewMode === "Rows" || model.ViewMode === "Columns" ? <span className="ml-1 fs-sm mr-4">{highlightedName}</span> : <span className="fs-xs ml-1 mr-4" style={{ maxWidth: '80%', textOverflow: 'ellipsis', overflowX: 'hidden' }}>{highlightedName}</span>}
                {extraContent ? extraContent : null}
            </>;
        }
    }, [model.Favourites, model.ViewMode, model.Search, model.Filter, model.SubFilter, extraContent, selected, prefab, prefab.Name, prefab.Type]);

    const borderClass = model.Filter !== "Favourite" && model.Favourites.includes(prefab.Name) ? " border-secondary-trans" : prefab.Meta && prefab.Meta.IsDangerous ? " border-danger-trans" : "";
    
    return <Button color={selected.Name == prefab.Name ? "primary" : "light"} style={selected.Name == prefab.Name ? "trans" : "trans-faded"} onMouseEnter={() => onInternalMouseEnter()} onMouseLeave={() => onInternalMouseLeave()} className={"asset-menu-item auto flex-1 m-mini" + borderClass + (selected.Name == prefab.Name ? " text-dark" : " text-light") + (model.ViewMode !== "IconGrid" && model.ViewMode !== "IconGridLarge" ? " flat" : "") + (model.ViewMode !== "IconGrid" && model.ViewMode !== "IconGridLarge" && selected.Name !== prefab.Name ? " btn-transparent" : "")} onClick={onSelectPrefab}>
        <div className={"d-flex align-items-center justify-content-center p-relative " + (model.ViewMode === "Columns" || model.ViewMode === "Rows" || model.ViewMode === "Detailed" ? " w-x flex-row " : " flex-column")}>
            {render()}
        </div>
    </Button>
};

export default PrefabItem;