angular.module("sacta_proxy")
    .controller("SprxConfigCtrl", function ($scope, $interval, $location, $serv, $lserv) {

        /** Inicializacion */
        var ctrl = this;
        ctrl.pagina = 0;
        ctrl.config = undefined;
        ctrl.selecteddep = "0";
        ctrl.selecteddep_mirror = "0";
        ctrl.dep = {
            cfg: null,
            isdep: false,
        };
        ctrl.topage = (page) => {
            ConsolidateFromPage(ctrl.pagina);
            DataLoadOfPage(page);
            ctrl.pagina = page;
        }
        ctrl.update = () => {
            if ($scope.cfgform.$valid) {
                alertify.confirm(DialogTitle, '�Confirma la actualizaci�n de la Configuraci�n',
                    () => {
                        $serv.config_save(ctrl.config)
                    },
                    () => {
                        alertify.success('Operacion Cancelada');
                    });
            }
            else {
                alertify.error("No se puede actualizar la configuraci�n. Existen campos no v�lidos.");
            }
            //alertify.success("Updating config..");
            //ConsolidateFromPage(ctrl.pagina);
        };
        ctrl.reset = () => {
            /** Obtiene de nuevo la configuracion del servidor */
            load_config(() => {
                /** reinicia los estados del form de configuracion */
                $scope.cfgform.$setPristine();
                DataLoadOfPage(ctrl.pagina);
            });
        };
        ctrl.changedep = () => {
            ConsolidateFromPage(ctrl.pagina);
            ctrl.selecteddep_mirror = ctrl.selecteddep;
            DataLoadOfPage(ctrl.pagina);
        };

        /**
         * 
         * @param {any} page
         */
        function ConsolidateFromPage(page) {
            if (page == 1) {
                ctrl.config.Psi = ctrl.dep.cfg;
            }
            else if (page == 2) {
                ctrl.config.Dependencies[ctrl.selecteddep_mirror] = ctrl.dep.cfg;
            }
        }
        /**
         * 
         * @param {any} page
         */
        function DataLoadOfPage(page) {
            if (page < 2) {
                if (ctrl.config) {
                    ctrl.dep.cfg = ctrl.config.Psi;
                    ctrl.dep.isdep = false;
                }
            }
            else if (page == 2) {
                if (ctrl.config) {
                    ctrl.dep.cfg = ctrl.config.Dependencies[ctrl.selecteddep_mirror];
                    ctrl.dep.isdep = true;
                }
            }
        }
        /**
         * 
         * @param {any} sync
         */
        function load_config(sync) {
            $serv.config((config) => {
                if (config.res == "ok") {
                    ctrl.config = config.cfg;
                    if (sync) sync();
                }
                else {
                    // 
                    alertify.error("Error: " + config.res + ". Al cargar la configuracion...");
                }
            });
        }

        /** */
        var timer = $interval(function () {
            /** Info para el estado de validacion del FORM */
            console.log("CfgForm: ", $scope.cfgform, $scope.cfgform.$dirty, $scope.cfgform.$valid);
        }, 5000);

        /** */
        $scope.$on('$viewContentLoaded', function () {
            load_config(() => {
                DataLoadOfPage(ctrl.pagina);
                $scope.cfgform.$setPristine();
            });
        });
        /** */
        var onRouteChangeOff = $scope.$on("$locationChangeStart", async function (event, newUrl, oldUrl) {
            var msg = 'Existen Modificaciones sin salvar. �Desea salir de la p�gina?. Las modificaciones se perderan';
            if ($scope.cfgform.$dirty) {
                alertify.confirm(DialogTitle, msg,
                    function () {
                        /** Continue */
                        onRouteChangeOff(); //Stop listening for location changes
                        $location.path($location.url(newUrl).hash()); //Go to page they're interested in
                    }
                    , function () {
                        /** Cancela*/
                        $lserv.MenuOption(1);
                    });
                //prevent navigation by default since we'll handle it
                //once the user selects a dialog option
                event.preventDefault();
            }
            return;
        });

    /** Salida del Controlador. Borrado de Variables */
        $scope.$on("$destroy", function () {
            $interval.cancel(timer);
        });
    });
