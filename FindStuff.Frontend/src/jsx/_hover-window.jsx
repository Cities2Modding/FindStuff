import React from "react";

const HoverWindow = ({ className, hoverPrefab, _L }) => {
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
    const renderTags = react.useCallback(() => {
        const getName = (tag) => {
            const key = `FindStuff.Tag.${tag}`;
            const name = _L(key);

            if (name === key)
                return tag;
            else
                return name;
        };

        return hoverPrefab.Tags ? hoverPrefab.Tags.map((tag, index) => <div key={index + "_" + tag} className="badge badge-dark">
            {getName(tag)}
        </div>) : null;
        
    }, [hoverPrefab.Tags, _L]);

    const renderHoverContents = react.useCallback( () => {
        if (!hoverPrefab)
            return;

        const computedDangerousReason = react.useMemo(() => dangerousReason(hoverPrefab), [hoverPrefab, hoverPrefab.Meta, hoverPrefab.Meta.IsDangerous, hoverPrefab.Meta.IsDangerousReason, _L]);

        return <Grid>
            <div className="col-3">
                <div className="bg-dark-trans-mid rounded-sm w-x">
                    <Icon icon={prefabIconSrc} fa={isFAIcon} className="icon-xxxl" />
                </div>
            </div>
            <div className="col-9">
                {prefabDescText ?
                    <p className="mb-4 fs-sm" cohinline="cohinline">
                        {prefabDescText}
                    </p> : null}
                {hoverPrefab.Meta && hoverPrefab.Meta.IsDangerous ? <div className="alert alert-danger fs-sm d-flex flex-row flex-wrap align-items-center p-2 mb-4">
                    <Icon className="mr-2" icon="solid-circle-exclamation" fa />
                    {computedDangerousReason}
                </div> : null}
                <div className="d-inline">
                    {renderTags()}
                </div>
            </div>
        </Grid>
    }, [hoverPrefab, prefabDescText]);

    const modalTypeIconIsFAIcon = hoverPrefab && hoverPrefab.TypeIcon ? hoverPrefab.TypeIcon.includes("fa:") : false;
    const modalTypeIconSrc = modalTypeIconIsFAIcon ? hoverPrefab.TypeIcon.replaceAll("fa:", "") : hoverPrefab ? hoverPrefab.TypeIcon : null;

    return <Modal className={className} icon={<><Icon icon={modalTypeIconSrc} fa={modalTypeIconIsFAIcon ? true : null} /></>} title={prefabName(hoverPrefab)} noClose>
        {renderHoverContents()}
    </Modal>;
};

export default HoverWindow;