Feature: Subir receta médica a una reserva
    Como cliente
    Quiero subir una receta médica a una reserva
    Para poder reservar medicamentos que requieren prescripción

Background:
    Given que el sistema tiene cargada la farmacia "Farmacia Central"
    And la "Farmacia Central" tiene el medicamento "Amoxicilina 500mg" que requiere receta con stock de 5 unidades

Scenario: Identificación visual de medicamento que requiere receta médica
    Given un usuario no autenticado visita la página de reservas "/reservations/create"
    And selecciona la farmacia "Farmacia Central" de la lista desplegable "#select-farmacia"
    When agrega 1 unidad del medicamento "Amoxicilina 500mg"
    Then la fila del medicamento debe mostrar la etiqueta ".tag-receta-requerida" con el texto "Requiere Receta"
    And debe mostrarse el contenedor de carga de archivos "#upload-zone-receta"

Scenario: Subida de receta y cambio a revisión por la farmacia
    Given un usuario no autenticado visita la página de reservas "/reservations/create"
    And selecciona la farmacia "Farmacia Central" de la lista desplegable "#select-farmacia"
    And agrega 1 unidad del medicamento "Amoxicilina 500mg"
    And completa el formulario con nombre "Carlos Gómez" y email "carlos@example.com"
    When arrastra el archivo "receta_medica_2026.pdf" al elemento input "#file-receta"
    And hace clic en el botón "#btn-confirmar-reserva"
    Then la reserva debe crearse con la etiqueta de estado ".estado-badge" conteniendo el texto "Pendiente"
    And el indicador de receta debe decir "Receta: Presentada - Pendiente de Validación"
