import { CommonModule } from "@angular/common";
import {
  Component,
  inject,
  OnDestroy,
  Output,
  EventEmitter,
} from "@angular/core";
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  AbstractControl,
  ValidationErrors,
} from "@angular/forms";
import {
  CreateRawMaterialRequest,
  RawMaterialCategory,
  RawMaterialResponse,
} from "../../../core/models/raw-material.model";

@Component({
  selector: "app-raw-material-form",
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: "./raw-material-form.component.html",
  styleUrl: "./raw-material-form.component.css",
})
export class RawMaterialFormComponent implements OnDestroy {
  @Output() formSubmit = new EventEmitter<CreateRawMaterialRequest>();
  @Output() formStatusChange = new EventEmitter<{
    isValid: boolean;
    isLoading: boolean;
  }>();

  private readonly fb = inject(FormBuilder);

  form: FormGroup;
  isLoading = false;
  submitted = false;
  serverError: string | null = null;
  categories = Object.values(RawMaterialCategory);

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
        supplier: ["", [Validators.required, Validators.minLength(2)]],
        cepNumber: [""],
        notes: [""],
      },
      { validators: this.minMaxValidator },
    );
  }

  ngOnDestroy(): void {
    // Clean up if needed
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

    if (this.form.invalid) {
      return;
    }

    this.isLoading = true;
    this.emitFormStatus();

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
      supplier: this.form.get("supplier")!.value,
      cepNumber: this.form.get("cepNumber")!.value || undefined,
      notes: this.form.get("notes")!.value || undefined,
    };

    this.formSubmit.emit(request);
  }

  onSubmitSuccess(response: RawMaterialResponse): void {
    this.isLoading = false;
    this.submitted = false;
    this.form.reset();
    this.emitFormStatus();
  }

  onSubmitError(error: any): void {
    this.isLoading = false;
    this.emitFormStatus();

    if (error.status === 409) {
      this.serverError =
        "A raw material with this name and pharmaceutical API already exists.";
    } else if (error.status === 400) {
      this.serverError =
        error.error?.detail || "Invalid data. Please check your inputs.";
    } else if (error.status === 403) {
      this.serverError =
        "You do not have permission to register raw materials.";
    } else {
      this.serverError =
        "An error occurred while registering the raw material. Please try again.";
    }
  }

  private emitFormStatus(): void {
    this.formStatusChange.emit({
      isValid: this.form.valid && !this.isLoading,
      isLoading: this.isLoading,
    });
  }

  getFieldError(fieldName: string): string | null {
    const control = this.form.get(fieldName);
    if (!control || !control.errors || !this.submitted) {
      return null;
    }

    if (control.hasError("required")) {
      return `${this.formatFieldName(fieldName)} is required.`;
    }
    if (control.hasError("minLength")) {
      return `${this.formatFieldName(fieldName)} must be at least 2 characters.`;
    }
    if (control.hasError("pattern")) {
      return `${this.formatFieldName(fieldName)} must be a valid number.`;
    }
    if (control.hasError("minMaxInvalid")) {
      return "Minimum specification limit must be less than or equal to maximum.";
    }

    return null;
  }

  private formatFieldName(field: string): string {
    return field
      .replace(/([A-Z])/g, " $1")
      .toLowerCase()
      .trim()
      .split(" ")
      .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
      .join(" ");
  }

  get isFormValid(): boolean {
    return this.form.valid && !this.isLoading;
  }
}
