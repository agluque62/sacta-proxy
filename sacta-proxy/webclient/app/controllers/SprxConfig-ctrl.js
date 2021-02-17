angular.module("sacta_proxy")
    .controller("SprxConfigCtrl", function ($scope, $interval, $serv, $lserv) {

        /** Inicializacion */
        var ctrl = this;
        ctrl.pagina = 0;
        ctrl.config = {};

        function load_config() {
            $serv.config((config) => {

            });
        }

        /** */
        $scope.$on('$viewContentLoaded', function () {
            load_config();
        });
        /** Salida del Controlador. Borrado de Variables */
        $scope.$on("$destroy", function () {

        });
    });
