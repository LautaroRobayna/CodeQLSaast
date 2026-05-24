Feature: Confirmar reserva
    Como empleado de farmacia
    Quiero confirmar reservas pendientes
    Para habilitar el retiro de medicamentos

Background:
    Given que el sistema tiene la reserva "RES-777" en estado "Pendiente"
    And la reserva "RES-777" contiene 3 unidades de "Paracetamol 500mg" y 2 unidades de "Amoxicilina 500mg"
    And el medicamento "Paracetamol 500mg" no requiere receta médica
    And el medicamento "Amoxicilina 500mg" requiere receta médica

Scenario: Confirmación exitosa de una reserva pendiente por un empleado de farmacia
    Given el empleado "Carlos" con rol "Empleado Farmacia" inicia sesión en el sistema
    And accede al panel de empleado en "/employee"
    And hace clic en "#btn-validar-reservas" para ir a la gestión de reservas
    And se encuentra en la página de validación "/employee/validate-reservations"
    And selecciona la reserva "RES-777" de la lista de pendientes
    And hace clic en el archivo "receta-amoxicilina.pdf" de la lista de recetas adjuntas
    And visualiza la receta de "Amoxicilina 500mg" en el visor ".visor-receta"
    When confirma la reserva haciendo clic en "#btn-confirmar-reserva-sistema"
    Then el sistema debe mostrar un mensaje modal con el texto "Reserva confirmada exitosamente"
    And el sistema debe cambiar el estado de la reserva a "Confirmed"
    And la reserva "RES-777" no debe aparecer en la lista de pendientes

  Scenario: Confirmación exitosa de una reserva sin receta médica
    Given una reserva pendiente sin receta médica
    And el empleado "Carlos" con rol "Empleado Farmacia" inicia sesión en el sistema
    And accede al panel de empleado en "/employee"
    And hace clic en "#btn-validar-reservas" para ir a la gestión de reservas
    And se encuentra en la página de validación "/employee/validate-reservations"
    And selecciona la reserva "RES-NO-RECIPE" de la lista de pendientes
    When confirma la reserva haciendo clic en "#btn-confirmar-reserva-sistema"
    Then el sistema debe mostrar un mensaje modal con el texto "Reserva confirmada exitosamente"
    And el sistema debe cambiar el estado de la reserva a "Confirmed"
    And la reserva "RES-NO-RECIPE" no debe aparecer en la lista de pendientes

  Scenario: Intento de confirmar una reserva que requiere receta médica sin archivo adjunto
    Given una reserva pendiente que requiere receta médica pero no tiene receta adjunta
    And el empleado "Carlos" con rol "Empleado Farmacia" inicia sesión en el sistema
    And accede al panel de empleado en "/employee"
    And hace clic en "#btn-validar-reservas" para ir a la gestión de reservas
    And se encuentra en la página de validación "/employee/validate-reservations"
    And selecciona la reserva "RES-NO-UPLOAD" de la lista de pendientes
    When confirma la reserva y la operacion falla
    Then el sistema debe mostrar un mensaje modal con el texto "Error al confirmar la reserva"
    And la reserva "RES-NO-UPLOAD" debe permanecer en la lista de pendientes

  Scenario: Cancelación de reserva sin receta médica
    Given una reserva pendiente sin receta médica
    And el empleado "Carlos" con rol "Empleado Farmacia" inicia sesión en el sistema
    And accede al panel de empleado en "/employee"
    And hace clic en "#btn-validar-reservas" para ir a la gestión de reservas
    And se encuentra en la página de validación "/employee/validate-reservations"
    And selecciona la reserva "RES-NO-RECIPE" de la lista de pendientes
    When hace clic en "#btn-rechazar-receta"
    Then el sistema debe mostrar un mensaje modal con el texto "Reserva rechazada"
    And el sistema debe cambiar el estado de la reserva a "Cancelled"
    And la reserva "RES-NO-RECIPE" no debe aparecer en la lista de pendientes

  Scenario: Rechazo de reserva por receta inválida
    Given el empleado "Carlos" con rol "Empleado Farmacia" inicia sesión en el sistema
    And accede al panel de empleado en "/employee"
    And hace clic en "#btn-validar-reservas" para ir a la gestión de reservas
    And se encuentra en la página de validación "/employee/validate-reservations"
    And selecciona la reserva "RES-777" de la lista de pendientes
    And hace clic en el archivo "receta-amoxicilina.pdf" de la lista de recetas adjuntas
    And visualiza la receta de "Amoxicilina 500mg" en el visor "#visor-receta-archivo"
    When hace clic en "#btn-rechazar-receta"
    Then el sistema debe mostrar un mensaje modal con el texto "Reserva rechazada"
    And el sistema debe cambiar el estado de la reserva a "Cancelled"
    And la reserva "RES-777" no debe aparecer en la lista de pendientes

  Scenario: Intento de confirmar una reserva no pendiente
    Given una reserva en estado "Confirmed" y otra en estado "Cancelled" en el sistema
    And el empleado "Carlos" con rol "Empleado Farmacia" inicia sesión en el sistema
    And accede al panel de empleado en "/employee"
    And hace clic en "#btn-validar-reservas" para ir a la gestión de reservas
    And se encuentra en la página de validación "/employee/validate-reservations"
    Then la lista de pendientes muestra el mensaje "No hay reservas pendientes"

  Scenario: Intento de rechazar una reserva no pendiente
    Given una reserva en estado "Confirmed" y otra en estado "Cancelled" en el sistema
    And el empleado "Carlos" con rol "Empleado Farmacia" inicia sesión en el sistema
    And accede al panel de empleado en "/employee"
    And hace clic en "#btn-validar-reservas" para ir a la gestión de reservas
    And se encuentra en la página de validación "/employee/validate-reservations"
    Then la lista de pendientes muestra el mensaje "No hay reservas pendientes"
