angular.module("sacta_proxy")
    .controller("SprxConfigCtrl", function ($scope, $interval, $serv, $lserv) {

        /** Inicializacion */
        var ctrl = this;
        ctrl.pagina = 0;
        ctrl.config = {};

        function load_config() {
            $serv.config().then(
                (response) => {
                    if (response.status == 200) {
                        if ((typeof response) == 'object') {

                        }
                        else {
                            // Seguramente ha vencido la sesion.
                        }
                    }
                    else {
                        // Error en el servidor.
                    }
                },
                (error) => {
                    // Error en el tratamiento de la peticion.
                }
            );
        }

        /** */
        $scope.$on('$viewContentLoaded', function () {
            load_config();
        });
        /** Salida del Controlador. Borrado de Variables */
        $scope.$on("$destroy", function () {

        });
    });
