import React from "react";

const LoadingScreen = ({ isVisible }) => {
    const react = window.$_gooee.react;
    const { Icon } = window.$_gooee.framework;

    const [visible, setVisible] = react.useState(isVisible);

    react.useEffect(() => {
        setVisible(isVisible);
    }, [isVisible]);

    return visible ? <div className="p-absolute w-100 h-100 p-left-0 p-top-0 bg-dark-trans d-flex align-items-center justify-content-center">
        <Icon icon="solid-spinner" className="icon-spin" fa />
    </div> : null;
};

export default LoadingScreen;