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
