/** */
angular.module("sacta_proxy")
    .controller("SprxAboutCtrl", function ($scope, $interval, $serv, $lserv) {

        /** Funcion Periodica del controlador */
        var timer = $interval(function () {

        }, pollingTime);

        /** Salida del Controlador. Borrado de Variables */
        $scope.$on("$destroy", function () {
            $interval.cancel(timer);
        });

        $scope.$on('$viewContentLoaded', function () {
            versiones();
        });

    /** Inicializacion */
        var ctrl = this;

        ctrl.pagina = 0;
        ctrl.version = {}
        ctrl.url_license = "http://" + window.location.hostname + ':' + window.location.port + '/COPYING.AUTHORIZATION.txt';

        /** Estados.. */
        ctrl.std = function () {
            return $lserv.GlobalStd();
        }

        ctrl.logs = function () {
            var win = window.open('logs/logfile.csv', '_blank');
            win.focus();
        };

        function versiones() {
            $serv.version((version) => {
                ctrl.version = version;
            });
        }


    });


