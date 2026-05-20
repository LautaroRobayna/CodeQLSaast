Feature: Crear una reserva
    Como cliente
    Quiero crear una reserva
    Para reservar los medicamentos de una farmacia

Background:
    Given que el sistema tiene cargada la farmacia "Farmacia Central"
    And la "Farmacia Central" tiene el medicamento "Paracetamol 500mg" con stock de 10 unidades
    And la "Farmacia Central" tiene el medicamento "Ibuprofeno 400mg" con stock de 3 unidades
    And la "Farmacia Central" tiene el medicamento "Amoxicilina 500mg" que requiere receta con stock de 5 unidades
    And existe la farmacia "Farmacia Norte" con el medicamento "Aspirina"
    And esta logueado con email "carlos@example.com" y contrasenia "12345"

Scenario: Creación exitosa de una reserva con medicamentos comunes
    Given un usuario no autenticado visita la página de reservas "/reservations/create"
    And selecciona la farmacia "Farmacia Central" de la lista desplegable "#select-farmacia"
    And agrega 3 unidades del medicamento "Paracetamol 500mg"
    And agrega 2 unidades del medicamento "Ibuprofeno 400mg"
    When completa el formulario de contacto con los siguientes datos:
    | Input Selector      | Valor               |
    | #nombre-completo    | Carlos Gómez        |
    | #email              | carlos@example.com  |
    And hace clic en el botón "#btn-confirmar-reserva"
    Then el sistema debe mostrar un mensaje en pantalla "Reserva creada exitosamente. Guarda tu clave pública."
    And el sistema debe mostrar la clave publica "PUB-12345"
    And la base de datos debe registrar la reserva en estado "Pendiente"
    And el stock en backend de "Paracetamol 500mg" debe actualizarse a 7 unidades
    And el stock en backend de "Ibuprofeno 400mg" debe actualizarse a 1 unidad
