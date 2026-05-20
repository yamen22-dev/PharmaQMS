import { CommonModule } from "@angular/common";
import {
  Component,
  inject,
} from "@angular/core";
import { RouterLink } from "@angular/router";
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  AbstractControl,
  ValidationErrors,
} from "@angular/forms";
import { RawMaterialService } from "../../../core/services/raw-material.service";
import {
  CreateRawMaterialRequest,
  RawMaterialCategory,
  RawMaterialResponse,
} from "../../../core/models/raw-material.model";

@Component({
  selector: "app-raw-material-form",
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: "./raw-material-form.component.html",
  styleUrls: ["./raw-material-form.component.css"],
})
export class RawMaterialFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly rawMaterialService = inject(RawMaterialService);

  form: FormGroup;
  isLoading = false;
  submitted = false;
  serverError: string | null = null;
  successMessage: string | null = null;
  showSuccessAlert = false;

  categoryOptions = Object.values(RawMaterialCategory).map((value) => ({
    value,
    label: this.getCategoryLabel(value),
  }));

  unitOptions = ["kg", "g", "mg", "l", "ml", "pcs"];

  constructor() {
    this.form = this.fb.group(
      {
        name: ["", [Validators.required, Validators.minLength(2)]],
        pharmaceuticalApi: ["", [Validators.required, Validators.minLength(2)]],
        category: ["", Validators.required],
        unit: ["", [Validators.required, Validators.minLength(1)]],
        minSpecificationLimit: [
          "",
          [Validators.required, Validators.pattern(/^\d+(\.\d{1,2})?$/)],
        ],
        maxSpecificationLimit: [
          "",
          [Validators.required, Validators.pattern(/^\d+(\.\d{1,2})?$/)],
        ],
        supplier: [""],
        cepNumber: [""],
        notes: [""],
      },
      { validators: this.minMaxValidator },
    );
  }

  minMaxValidator(control: AbstractControl): ValidationErrors | null {
    const minValue = parseFloat(
      control.get("minSpecificationLimit")?.value || 0,
    );
    const maxValue = parseFloat(
      control.get("maxSpecificationLimit")?.value || 0,
    );
    const maxControl = control.get("maxSpecificationLimit");

    if (!maxControl) {
      return null;
    }

    if (minValue >= 0 && maxValue >= 0 && minValue > maxValue) {
      const existing = maxControl.errors ?? {};
      if (!existing["minMaxInvalid"]) {
        maxControl.setErrors({ ...existing, minMaxInvalid: true });
      }
      return { minMaxInvalid: true };
    }

    const existing = maxControl.errors ?? {};
    if (existing["minMaxInvalid"]) {
      delete existing["minMaxInvalid"];
      maxControl.setErrors(Object.keys(existing).length ? existing : null);
    }

    return null;
  }

  onSubmit(): void {
    this.submitted = true;
    this.serverError = null;
    this.showSuccessAlert = false;
    this.successMessage = null;

    if (this.form.invalid) {
      return;
    }

    this.isLoading = true;

    const request: CreateRawMaterialRequest = {
      name: this.form.get("name")!.value,
      pharmaceuticalApi: this.form.get("pharmaceuticalApi")!.value,
      category: this.form.get("category")!.value,
      unit: this.form.get("unit")!.value,
      minSpecificationLimit: parseFloat(
        this.form.get("minSpecificationLimit")!.value,
      ),
      maxSpecificationLimit: parseFloat(
        this.form.get("maxSpecificationLimit")!.value,
      ),
      supplier: this.form.get("supplier")!.value ?? "",
      cepNumber: this.form.get("cepNumber")!.value || undefined,
      notes: this.form.get("notes")!.value || undefined,
    };

    this.rawMaterialService.createRawMaterial(request).subscribe({
      next: (response: RawMaterialResponse) => {
        this.isLoading = false;
        this.submitted = false;
        this.form.reset();
        this.showSuccessAlert = true;
        this.successMessage = `Grondstof "${response.name}" succesvol geregistreerd.`;
      },
      error: (error) => {
        this.onSubmitError(error);
      },
    });
  }

  onSubmitError(error: any): void {
    this.isLoading = false;
    this.showSuccessAlert = false;
    this.successMessage = null;

    if (error.status === 409) {
      this.serverError =
        "Er bestaat al een grondstof met deze naam en farmaceutische API.";
    } else if (error.status === 400) {
      this.serverError =
        error.error?.detail || "Ongeldige invoer. Controleer de velden.";
    } else if (error.status === 403) {
      this.serverError =
        "Je hebt geen rechten om grondstoffen te registreren.";
    } else {
      this.serverError =
        "Er ging iets mis bij het registreren. Probeer het opnieuw.";
    }
  }

  getFieldError(fieldName: string): string | null {
    const control = this.form.get(fieldName);
    if (!control || !control.errors || !this.submitted) {
      return null;
    }

    if (control.hasError("required")) {
      return `${this.formatFieldName(fieldName)} is verplicht.`;
    }
    if (control.hasError("minlength") || control.hasError("minLength")) {
      return `${this.formatFieldName(fieldName)} moet minimaal 2 tekens zijn.`;
    }
    if (control.hasError("pattern")) {
      return `${this.formatFieldName(fieldName)} moet een geldig getal zijn.`;
    }
    if (control.hasError("minMaxInvalid")) {
      return "Minimum specificatiegrens moet kleiner of gelijk zijn aan maximum.";
    }

    return null;
  }

  private formatFieldName(field: string): string {
    const labels: Record<string, string> = {
      name: "Naam grondstof",
      pharmaceuticalApi: "Farmaceutische API",
      category: "Categorie",
      unit: "Eenheid",
      minSpecificationLimit: "Min. specificatiegrens",
      maxSpecificationLimit: "Max. specificatiegrens",
      notes: "Omschrijving",
    };

    return labels[field] ?? field;
  }

  get isFormValid(): boolean {
    return this.form.valid && !this.isLoading;
  }

  getCategoryLabel(category: string): string {
    switch (category) {
      case RawMaterialCategory.ActivePharmaceuticalIngredient:
        return "Werkzame stof";
      case RawMaterialCategory.Excipient:
        return "Hulpstof";
      case RawMaterialCategory.Packaging:
        return "Verpakking";
      case RawMaterialCategory.Solvent:
        return "Oplosmiddel";
      case RawMaterialCategory.Other:
        return "Overig";
      default:
        return category;
    }
  }
}
