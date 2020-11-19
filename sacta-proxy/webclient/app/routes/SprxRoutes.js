/** */
angular
    .module('sacta_proxy')
    .config(config);

function config($routeProvider) {
    $routeProvider
        .when(routeDefault, {
            templateUrl: 'app/views/SprxStatus.html',
            controller: 'SprxStatusCtrl',
            controllerAs: 'ctrl'
        })
        .when(routeConfig, {
            templateUrl: 'app/views/SprxConfig.html',
            controller: 'SprxConfigCtrl',
            controllerAs: 'ctrl'
        });
    // .when(routeForUnauthorizedAccess, {
    // templateUrl: 'app/views/session-expired.html'
    // // templateUrl: 'session-expired.html'
    // }
    // );
}

