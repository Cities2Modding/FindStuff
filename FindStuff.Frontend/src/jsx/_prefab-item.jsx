import React from "react";

const PrefabItem = ({ model, trigger, prefab, selected, _L, onSelected, onMouseEnter, onMouseLeave, extraContent }) => {
    const react = window.$_gooee.react;
    const { Icon, Grid, Button } = window.$_gooee.framework;

    const isTypeFAIcon = prefab.TypeIcon.includes("fa:");
    const iconSrc = react.useMemo(() => isTypeFAIcon ? prefab.TypeIcon.replace("fa:", "") : prefab.TypeIcon, [prefab.TypeIcon]);

    const isFAIcon = prefab.Thumbnail.includes("fa:");
    const prefabIconSrc = react.useMemo(() => isFAIcon ? prefab.Thumbnail.replace("fa:", "") : prefab.Thumbnail, [prefab.Thumbnail]);
    
    const prefabName = () => {
        const key = `Assets.NAME[${prefab.Name}]`;
        const name = _L(key);

        if (name === key)
            return prefab.Name;
        else
            return name;
    };

    const prefabTypeName = () => {
        const key = `FindStuff.PrefabType.${prefab.Type}`;
        const name = _L(key);

        if (name === key)
            return prefab.Type;
        else
            return name;
    };

    const computedPrefabName = react.useMemo(() => prefabName(), [prefab.Name, _L]);
    const computedPrefabTypeName = react.useMemo(() => prefabTypeName(), [prefab.Type, _L]);

    const highlightSearchTerm = (text) => {
        // Check if there's a search term; if not, return the original text
        if (!model.Search || model.Search.trim().length === 0) return text;

        // Split the search term into individual words and escape regex special characters
        const words = model.Search.trim().split(/\s+/).map(word =>
            word.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
        );

        // Create a regex to match any of the words, case-insensitively
        const regex = new RegExp(`(${words.join('|')})`, 'gi');

        // Split the text by the regex, keeping matched parts for highlighting
        const splitText = text.split(regex);

        // Map through the split text to highlight matched parts
        return splitText.map((part, index) =>
            regex.test(part) ? (
                <span key={index} className={selected.Name === prefab.Name ? "text-dark bg-warning" : "text-dark bg-warning"}>
                    <b>{part}</b>
                </span>
            ) : part
        );
    };

    const highlightedName = react.useMemo(() => highlightSearchTerm(computedPrefabName), [computedPrefabName, model.Search]);
    const highlightedType = react.useMemo(() => highlightSearchTerm(computedPrefabTypeName), [computedPrefabTypeName, model.Search]);

    const onSelectPrefab = () => {
        if (!model.ExpertMode && prefab.IsExpertMode)
            return;

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
    function formatNumber(number) {
        var parts = number.toString().split(".");
        parts[0] = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ",");
        return parts.join(".");
    }

    const render = react.useCallback(() => {
        if (model.ViewMode == "Detailed") {
            return <>
                <Grid>
                    <div className="col-5">
                        <div className="d-flex flex-row" style={{ overflowX: "hidden" }}>
                            {prefabIconSrc === iconSrc ? <Icon className="icon-sm ml-1 mr-1" icon={prefabIconSrc} fa={isFAIcon ? true : null} /> : <img className="icon icon-sm ml-1 mr-1" src={prefab.Thumbnail} />}
                            <span className="fs-sm flex-1">{highlightedName}</span>
                        </div>
                    </div>
                    <div className="col-3">
                        <span className="fs-xs h-x" style={{ overflowX: "hidden" }}>
                            <Icon icon={iconSrc} fa={isTypeFAIcon ? true : null} size="sm" className={(isTypeFAIcon ? "bg-muted " : "") + "mr-1"} style={{ maxHeight: "16rem" }} />
                            {highlightedType}
                        </span>
                    </div>
                    <div className="col-3">
                        <div className="d-flex flex-row align-items-start flex-wrap">
                            {prefab.Meta && prefab.Meta.IsDangerous ? <div className="badge badge-xs badge-danger">Dangerous</div> : null}
                            {prefab.Meta && prefab.Meta.ZoneLotWidth ? <div className="badge badge-xs badge-black">{prefab.Meta.ZoneLotWidth}x{prefab.Meta.ZoneLotDepth}</div> : null}
                            {prefab.Meta && prefab.Meta.Cost ? <div className="badge badge-xs badge-black text-secondary">&#162;{formatNumber(prefab.Meta.Cost)}</div> : null}
                            {prefab.Meta && prefab.Meta.XPReward ? <div className="badge badge-xs badge-black text-success">{formatNumber(prefab.Meta.XPReward)}&nbsp;XP</div> : null}
                        </div>
                    </div>
                    <div className="w-5 p-relative pr-2">
                        {extraContent ? extraContent : null}
                    </div>
                </Grid>
            </>
        }
        else {
            return <>
                {prefabIconSrc ? <Icon className={model.ViewMode === "IconGrid" ? "icon-lg" : model.ViewMode === "IconGridLarge" ? "icon-xl" : "icon-sm ml-2"} icon={prefabIconSrc} fa={isFAIcon ? true : null} /> : <img className={model.ViewMode === "IconGrid" ? "icon icon-lg" : model.ViewMode === "IconGridLarge" ? "icon icon-xl" : "icon icon-sm ml-2"} src={prefab.Thumbnail} />}
                {model.ViewMode === "Rows" || model.ViewMode === "Columns" ? <span className="ml-1 fs-sm mr-4">{highlightedName}</span> : <span className="fs-xs ml-1 mr-4" style={{ maxWidth: '80%', textOverflow: 'ellipsis', overflowX: 'hidden' }}>{highlightedName}</span>}
                {extraContent ? extraContent : null}
            </>;
        }
    }, [model.Favourites, model.ViewMode, model.Search, model.Filter, model.SubFilter, extraContent, selected, prefab, prefab.Name, prefab.Type]);

    const borderClass = model.Filter !== "Favourite" && model.Favourites.includes(prefab.Name) ? " border-secondary-trans" : prefab.Meta && prefab.Meta.IsDangerous ? " border-danger-trans" : prefab.IsModded ? "border-info-trans" : "";
    
    return <Button color={selected.Name == prefab.Name ? "primary" : "light"} style={selected.Name == prefab.Name ? "trans" : "trans-faded"} onMouseEnter={() => onInternalMouseEnter()} onMouseLeave={() => onInternalMouseLeave()} className={"asset-menu-item auto flex-1 m-mini" + borderClass + (selected.Name == prefab.Name ? " text-dark" : " text-light") + (model.ViewMode !== "IconGrid" && model.ViewMode !== "IconGridLarge" ? " flat" : "") + (model.ViewMode !== "IconGrid" && model.ViewMode !== "IconGridLarge" && selected.Name !== prefab.Name ? " btn-transparent" : "") + (!model.ExpertMode && prefab.IsExpertMode ? " opacity-50" : "")} onClick={onSelectPrefab}>
        <div className={"d-flex align-items-center justify-content-center p-relative " + (model.ViewMode === "Columns" || model.ViewMode === "Rows" || model.ViewMode === "Detailed" ? " w-x flex-row " : " flex-column")}>
            {render()}
        </div>
    </Button>
};

export default PrefabItem;