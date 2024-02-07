import React from "react";

const HoverWindow = ({ hoverPrefab, _L }) => {
    const react = window.$_gooee.react;
    const { Icon, Grid, Modal } = window.$_gooee.framework;
    
    const prefabDesc = (p) => {
        if (!p)
            return null;

        const key = `Assets.DESCRIPTION[${p.Name}]`;
        const trans = _L(key);

        if (trans === key)
            return null;

        return trans;
    };

    const prefabDescText = react.useMemo(() => prefabDesc(hoverPrefab), [hoverPrefab, _L]);

    const renderHoverContents = react.useCallback( () => {
        if (!hoverPrefab)
            return;

        return <Grid>
            <div className="col-3">
                <Icon icon={hoverPrefab.Thumbnail} size="xxl" />
            </div>
            <div className="col-9">
                {prefabDescText ?
                    <p className="mb-4 fs-sm" cohinline="cohinline">
                        {prefabDescText}
                    </p> : null}
                {hoverPrefab.Meta && hoverPrefab.Meta.IsDangerous ? <div className="alert alert-danger fs-sm d-flex flex-row flex-wrap align-items-center p-2 mb-4">
                    <Icon className="mr-2" icon="solid-circle-exclamation" fa />
                    {hoverPrefab.Meta.IsDangerousReason}
                </div> : null}
                <div className="d-inline">
                    {hoverPrefab.Tags ? hoverPrefab.Tags.map((tag, index) => <div key={index} className="badge badge-info">
                        {tag}
                    </div>) : null}
                </div>
            </div>
        </Grid>
    }, [hoverPrefab, prefabDescText]);

    const modalTypeIconIsFAIcon = hoverPrefab && hoverPrefab.TypeIcon ? hoverPrefab.TypeIcon.includes("fa:") : false;
    const modalTypeIconSrc = modalTypeIconIsFAIcon ? hoverPrefab.TypeIcon.replaceAll("fa:", "") : hoverPrefab ? hoverPrefab.TypeIcon : null;

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

    return <Modal className="mb-2" icon={<><Icon icon={modalTypeIconSrc} fa={modalTypeIconIsFAIcon ? true : null} /></>} title={prefabName(hoverPrefab)} noClose>
        {renderHoverContents()}
    </Modal>;
};

export default HoverWindow;