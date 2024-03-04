import React from "react";

const HoverWindow = ({ className, model, hoverPrefab, _L }) => {
    const react = window.$_gooee.react;
    const { Icon, Grid, Modal } = window.$_gooee.framework;

    const prefabName = (p) => {
        if (!p)
            return null;

        const key = `Assets.NAME[${p.Name}]`;
        const name = _L(key);

        if (name === key)
            return p.Name;

        else
            return name;
    };

    const prefabDesc = (p) => {
        if (!p)
            return null;

        const key = `Assets.DESCRIPTION[${p.Name}]`;
        const trans = _L(key);

        if (trans === key)
            return null;

        return trans;
    };

    const dangerousReason = (p) => {
        if (!p || !p.Meta || !p.Meta.IsDangerous)
            return null;
            
        const text = _L(p.Meta.IsDangerousReason);

        if (text === p.Meta.IsDangerousReason)
            return p.Meta.IsDangerousReason;

        else
            return text;
    };
    const isFAIcon = hoverPrefab && hoverPrefab.Thumbnail.includes("fa:");
    const prefabIconSrc = react.useMemo(() => isFAIcon ? hoverPrefab.Thumbnail.replace("fa:", "") : hoverPrefab.Thumbnail, [hoverPrefab.Thumbnail]);

    const prefabDescText = react.useMemo(() => prefabDesc(hoverPrefab), [hoverPrefab, _L]);

    const highlightSearchTerm = (text) => {
        const regex = new RegExp(`(${model.Search})`, 'gi');
        const splitText = text.split(regex);

        return (!model.Search || model.Search.length == 0) ? text : splitText.map((part, index) =>
            regex.test(part) ? <span key={index}>
                <b className="text-dark bg-warning">{part}</b>
            </span> : part
        );
    };

    const renderTags = react.useCallback(() => {
        const getName = (tag) => {
            const key = `FindStuff.Tag.${tag}`;
            const name = _L(key);

            if (name === key)
                return tag;
            else
                return name;
        };
                
        return hoverPrefab.Tags ? hoverPrefab.Tags.map((tag, index) => <span key={index + "_" + tag} className="badge badge-dark">
            {highlightSearchTerm(getName(tag))}
        </span>) : null;
        
    }, [hoverPrefab.Tags, _L, model.Search]);

    const formatNumber = (number) => {
        var parts = number.toString().split(".");
        parts[0] = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ",");
        return parts.join(".");
    };

    const metaContent = <div className="mt-4 fs-sm">
        <Grid auto>
            <div>
                {hoverPrefab.Meta.XPReward ? <div className="d-flex flex-row justify-content-center bg-section-light rounded p-1">
                    <span className="text-light mr-1">{formatNumber(hoverPrefab.Meta.XPReward)}</span>
                    <span className="text-muted">XP</span>
                </div> : null}
            </div>
            <div></div>
            <div></div>
        </Grid>
    </div>;

    const renderHoverContents = react.useCallback( () => {
        if (!hoverPrefab)
            return;

        const computedDangerousReason = react.useMemo(() => dangerousReason(hoverPrefab), [hoverPrefab, hoverPrefab.Meta, hoverPrefab.Meta.IsDangerous, hoverPrefab.Meta.IsDangerousReason, _L]);

        return <>
            <div className="d-flex flex-row align-items-start justify-content-start">
                <div className="bg-dark-trans-mid rounded-sm w-x mr-3 ml-3">
                    <Icon icon={prefabIconSrc} fa={isFAIcon} className="icon-xxxl" />
                </div>
                <div className="flex-1 ml-3">
                    {prefabDescText ?
                        <p className="mb-4 fs-sm" cohinline="cohinline">
                            {prefabDescText}
                        </p> : null}
                    {hoverPrefab.Meta && hoverPrefab.Meta.IsDangerous ? <div className="alert alert-danger fs-sm d-inline p-2 mb-4">
                        <Icon className="mr-2" icon="solid-circle-exclamation" fa />
                        <div className="flex-1 d-flex flex-column align-items-start justify-content-start">
                            <p cohinline="cohinline">
                                <b>{computedDangerousReason}</b>
                            </p>
                            {!model.ExpertMode && hoverPrefab.IsExpertMode ? <p className="mt-0 mb-0" cohinline="cohinline">
                                You need to enable Expert Mode in the Find Stuff options menu to use this asset.
                            </p> : null}
                        </div>
                    </div> :
                        !model.ExpertMode && hoverPrefab.IsExpertMode ? <div className="alert alert-danger fs-sm d-inline p-2 mb-4">
                            <Icon className="mr-2" icon="solid-circle-exclamation" fa />
                            <p className="flex-1" cohinline="cohinline">
                                You need to enable Expert Mode in the Find Stuff options menu to use this asset.
                            </p>
                        </div> : null}
                    {hoverPrefab.Meta && hoverPrefab.Meta.BuildingStaticUpgrade ? <div className="alert alert-info fs-sm d-inline p-2 mb-4">
                        <Icon className="mr-2" icon="solid-circle-exclamation" fa />
                        <div className="flex-1 d-flex flex-column align-items-start justify-content-start">
                            <p cohinline="cohinline">
                                <b>
                                    {_L("FindStuff.HowToBuild")}
                                </b>
                            </p>
                            <p className="mt-0 mb-0" cohinline="cohinline">
                                {_L("FindStuff.BuildingStaticUpgrade_desc")}
                            </p>
                        </div>
                    </div> : null}
                    <p className="m-0" cohinline="cohinline">
                        {renderTags()}
                    </p>
                    {hoverPrefab.Meta ? metaContent : null}
                </div>
            </div>
        </>;
    }, [hoverPrefab, prefabDescText, model.Search]);

    const modalTypeIconIsFAIcon = hoverPrefab && hoverPrefab.TypeIcon ? hoverPrefab.TypeIcon.includes("fa:") : false;
    const modalTypeIconSrc = modalTypeIconIsFAIcon ? hoverPrefab.TypeIcon.replaceAll("fa:", "") : hoverPrefab ? hoverPrefab.TypeIcon : null;
    
    const titleContent = <div className="d-flex flex-row align-items-center justify-content-start fs-normal">
        <Icon className="mr-2 ml-2" icon={modalTypeIconSrc} fa={modalTypeIconIsFAIcon ? true : null} />
        <span className="flex-1 mr-2">{prefabName(hoverPrefab)}</span>
        {hoverPrefab.Meta.Cost ? <span className="ml-x">
            <span className="text-success mr-2">&#162;{formatNumber(hoverPrefab.Meta.Cost)}</span>
            <Icon className="bg-success mr-2" icon="money" mask />
        </span> : null}
    </div>;

    return <Modal className={className} title={titleContent} noClose>
        {renderHoverContents()}
    </Modal>;
};

export default HoverWindow;