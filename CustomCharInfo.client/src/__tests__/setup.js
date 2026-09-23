// jsdom has no window.scrollTo, which the router's scrollBehavior calls after every navigation.
window.scrollTo = () => {}
