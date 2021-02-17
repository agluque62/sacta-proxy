angular.module("sacta_proxy")
    .controller("SprxConfigCtrl", function ($scope, $interval, $serv, $lserv) {

        /** Inicializacion */
        var ctrl = this;
        ctrl.pagina = 0;
        ctrl.config = { General: { WebPort: 1234, WebActivityMinTimeout: 45, ActivateSactaLogic: "OR" } };
        ctrl.update = () => {
            alertify.success("Updating config..")

        };

        function load_config() {
            $serv.config((config) => {
                if (config.res == "ok") {
                    ctrl.config = config.cfg;
                }
                else {
                    // todo
                }
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
