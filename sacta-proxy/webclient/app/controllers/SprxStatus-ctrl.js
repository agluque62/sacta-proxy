/** */
angular.module("sacta_proxy")
    .controller("SprxStatusCtrl", function ($scope, $interval, $serv, $lserv) {
        /** Inicializacion */
        var ctrl = this;

        ctrl.pagina = 0;

        /** Estados.. */
        ctrl.std = function () {
            return $lserv.GlobalStd();
        }

        ctrl.logs = function () {
            var win = window.open('logs/logfile.csv', '_blank');
            win.focus();
        };

        /** Funcion Periodica del controlador */
        var timer = $interval(function () {

        }, pollingTime);

        /** Salida del Controlador. Borrado de Variables */
        $scope.$on("$destroy", function () {
            $interval.cancel(timer);
        });

    });


